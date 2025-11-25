using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class ThrottleGrabLocomotionSimple : MonoBehaviour
{
    public enum ThrottleAxis
    {
        Roll,
        Pitch,
        Yaw
    }

    [Header("Referencias")]
    public Transform xrOrigin;      // XR Origin (XR Rig)
    public Transform headTransform; // Main Camera (cabeza)
    public Transform bodyParent;    // Camera Offset o Main Camera (donde se pega el cubo)
    public Transform mountPoint;    // Punto sobre la moto donde se debe colocar el XR Origin
    [Header("Acciones de entrada")]
    public InputActionProperty brakeTriggerAction; // Gatillo trasero mano izquierda

    [Header("Movimiento tipo moto")]
    public float maxSpeed = 3f;        // Velocidad máxima (m/s)
    public float maxThrottleAngle = 45f; // Grados para gas máximo
    public float deadZoneAngle = 5f;     // Zona muerta para no vibrar
    public ThrottleAxis throttleAxis = ThrottleAxis.Roll;
    public bool invertAxis;
    public float accelerationRate = 6f;   // m/s por segundo al acelerar
    public float brakeRate = 10f;         // m/s por segundo al frenar girando hacia atrás
    public float naturalDrag = 2f;        // m/s por segundo cuando no hay input

    [Header("Freno con gatillo")]
    public float triggerBrakeStrength = 20f; // m/s por segundo aplicados por el gatillo
    public float triggerBrakeSmoothing = 4f;  // Qué tan rápido sigue la presión real
    public float triggerBrakeExponent = 2f;   // >1 reduce sensibilidad al inicio
    public AnimationCurve triggerBrakeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Header("Giro por inclinación de cabeza")]
    public float leanSensitivity = 40f;   // Grados para giro máximo
    public float maxTurnSpeed = 60f;      // Grados por segundo al inclinarse
    public float leanDeadZone = 5f;

    [Header("Debug")]
    public bool showDebug = true;
    [SerializeField] private float lastAngle;
    [SerializeField] private float lastForwardInput;
    [SerializeField] private float lastBrakeInput;
    [SerializeField] private float lastTriggerBrake;
    [SerializeField] private float currentSpeed;
    private float triggerBrakeFiltered;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private Transform originalParent;
    private Vector3 xrOriginInitialPosition;
    private Quaternion xrOriginInitialRotation;
    private bool isGrabbed = false;
    private Transform interactorTransform;  // Mano / mando que lo coge
    private Quaternion startRotation;       // Rotación mundial de referencia al coger
    private Quaternion startLocalRotation;  // Rotación relativa al XR Origin al coger

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        originalParent = transform.parent;

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnEnable()
    {
        var action = brakeTriggerAction.action;
        if (action != null && action.enabled == false)
        {
            action.Enable();
        }
    }

    void OnDisable()
    {
        var action = brakeTriggerAction.action;
        if (action != null && action.enabled)
        {
            action.Disable();
        }
    }

    void OnDestroy()
    {
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        interactorTransform = args.interactorObject.GetAttachTransform(grab);
        if (interactorTransform == null)
        {
            interactorTransform = args.interactorObject.transform;
        }

        // Guardar posición y rotación iniciales del XR Origin
        if (xrOrigin != null)
        {
            xrOriginInitialPosition = xrOrigin.position;
            xrOriginInitialRotation = xrOrigin.rotation;

            if (mountPoint != null)
            {
                xrOrigin.position = mountPoint.position;
                xrOrigin.rotation = mountPoint.rotation;
            }
        }

        // Rotación global y relativa al empezar a cogerlo
        startRotation = interactorTransform.rotation;
        if (xrOrigin != null)
        {
            startLocalRotation = Quaternion.Inverse(xrOrigin.rotation) * interactorTransform.rotation;
        }
        else
        {
            startLocalRotation = startRotation;
        }

        // Quitar física
        rb.useGravity = false;
        rb.isKinematic = true;

        // Pegar al cuerpo/cabeza
        if (bodyParent != null)
            transform.SetParent(bodyParent, true);

        if (showDebug)
            Debug.Log("[Throttle] Agarrado. Empezando a leer giro.");
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
        interactorTransform = null;

        // Volver a padre original
        transform.SetParent(originalParent, true);

        // Volver a física normal
        rb.isKinematic = false;
        rb.useGravity = true;

        // Devolver XR Origin a la posición inicial
        if (xrOrigin != null)
        {
            xrOrigin.position = xrOriginInitialPosition;
            xrOrigin.rotation = xrOriginInitialRotation;
        }

        if (showDebug)
            Debug.Log("[Throttle] Soltado. Parando movimiento.");
    }

    void Update()
    {
        if (!isGrabbed || interactorTransform == null || xrOrigin == null || headTransform == null)
            return;

        // 1. Calcular cuánto he girado el mando desde que lo cogí (en espacio del XR Origin)
        Quaternion currentLocalRotation = Quaternion.Inverse(xrOrigin.rotation) * interactorTransform.rotation;
        Quaternion delta = Quaternion.Inverse(startLocalRotation) * currentLocalRotation;
        Vector3 deltaEuler = delta.eulerAngles;

        float axisAngle = throttleAxis switch
        {
            ThrottleAxis.Roll => deltaEuler.z,
            ThrottleAxis.Pitch => deltaEuler.x,
            ThrottleAxis.Yaw => deltaEuler.y,
            _ => deltaEuler.z
        };

        if (axisAngle > 180f)
            axisAngle -= 360f;

        if (invertAxis)
            axisAngle = -axisAngle;

        float throttleForward = 0f;
        float brakeInput = 0f;
        float forwardAngle = Mathf.Max(0f, -axisAngle);
        float backwardAngle = Mathf.Max(0f, axisAngle);

        if (forwardAngle > deadZoneAngle)
        {
            throttleForward = Mathf.InverseLerp(deadZoneAngle, maxThrottleAngle, Mathf.Min(maxThrottleAngle, forwardAngle));
        }

        if (backwardAngle > deadZoneAngle)
        {
            brakeInput = Mathf.InverseLerp(deadZoneAngle, maxThrottleAngle, Mathf.Min(maxThrottleAngle, backwardAngle));
        }

        float triggerBrakeInput = 0f;
        if (brakeTriggerAction.action != null)
        {
            triggerBrakeInput = Mathf.Clamp01(brakeTriggerAction.action.ReadValue<float>());
        }

        float triggerBrakeShaped = triggerBrakeInput;
        if (triggerBrakeExponent > 0.01f)
        {
            triggerBrakeShaped = Mathf.Pow(triggerBrakeInput, triggerBrakeExponent);
        }

        float triggerBrakeTarget = triggerBrakeCurve != null
            ? Mathf.Clamp01(triggerBrakeCurve.Evaluate(triggerBrakeShaped))
            : triggerBrakeShaped;

        triggerBrakeFiltered = Mathf.MoveTowards(
            triggerBrakeFiltered,
            triggerBrakeTarget,
            Time.deltaTime * Mathf.Max(0.01f, triggerBrakeSmoothing));

        float triggerBrakeEffective = triggerBrakeFiltered;

        lastAngle = axisAngle;
        lastForwardInput = throttleForward;
        lastBrakeInput = brakeInput;
        lastTriggerBrake = triggerBrakeEffective;

        // Actualizar velocidad actual combinando aceleración y frenado en la misma ecuación
        float accelPerSecond = accelerationRate * throttleForward;
        float brakePerSecond = naturalDrag;
        if (brakeInput > 0f)
        {
            brakePerSecond += brakeRate * brakeInput;
        }
        if (triggerBrakeEffective > 0f)
        {
            brakePerSecond += triggerBrakeStrength * triggerBrakeEffective;
        }

        float netAcceleration = accelPerSecond - brakePerSecond; // positivo acelera, negativo frena

        if (showDebug)
        {
            Debug.Log($"[Throttle] Axis({throttleAxis}): {axisAngle:F1}  Forward: {throttleForward:F2}  Brake: {brakeInput:F2}  TriggerRaw: {triggerBrakeInput:F2}  TriggerShaped: {triggerBrakeShaped:F2}  TriggerEval: {triggerBrakeEffective:F2}  NetAcc: {netAcceleration:F2}");
        }

        currentSpeed = Mathf.Clamp(currentSpeed + netAcceleration * Time.deltaTime, 0f, maxSpeed);

        if (currentSpeed <= 0.01f)
            return;

        // 2. Girar XR Origin según inclinación de cabeza (roll)
        ApplyHeadLeanTurn();

        // 3. Dirección = hacia donde mira la cabeza (solo en plano XZ)
        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            return;
        forward.Normalize();

        // 3. Aplicar movimiento al XR Origin
        Vector3 motion = forward * currentSpeed * Time.deltaTime;

        xrOrigin.position += motion;
    }

    private void ApplyHeadLeanTurn()
    {
        if (leanSensitivity <= leanDeadZone || maxTurnSpeed <= 0f)
            return;

        float rollAngle = headTransform.localEulerAngles.z;
        if (rollAngle > 180f)
            rollAngle -= 360f;

        float absRoll = Mathf.Abs(rollAngle);
        if (absRoll <= leanDeadZone)
            return;

        float leanPercent = Mathf.Clamp01((absRoll - leanDeadZone) / (leanSensitivity - leanDeadZone));
        float turnDirection = Mathf.Sign(rollAngle);
        float yawDelta = -turnDirection * leanPercent * maxTurnSpeed * Time.deltaTime;

        xrOrigin.Rotate(Vector3.up, yawDelta, Space.World);
    }
}
