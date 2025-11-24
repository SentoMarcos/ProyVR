using UnityEngine;
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

    [Header("Movimiento tipo moto")]
    public float maxSpeed = 3f;        // Velocidad máxima (m/s)
    public float maxThrottleAngle = 45f; // Grados para gas máximo
    public float deadZoneAngle = 5f;     // Zona muerta para no vibrar
    public ThrottleAxis throttleAxis = ThrottleAxis.Roll;
    public bool invertAxis;
    public float accelerationRate = 6f;   // m/s por segundo al acelerar
    public float brakeRate = 10f;         // m/s por segundo al frenar girando hacia atrás
    public float naturalDrag = 2f;        // m/s por segundo cuando no hay input

    [Header("Debug")]
    public bool showDebug = true;
    [SerializeField] private float lastAngle;
    [SerializeField] private float lastForwardInput;
    [SerializeField] private float lastBrakeInput;
    [SerializeField] private float currentSpeed;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private Transform originalParent;
    private bool isGrabbed = false;
    private Transform interactorTransform;  // Mano / mando que lo coge
    private Quaternion startRotation;       // Rotación de referencia al coger

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        originalParent = transform.parent;

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
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

        // Rotación global al empezar a cogerlo
        startRotation = interactorTransform.rotation;

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

        if (showDebug)
            Debug.Log("[Throttle] Soltado. Parando movimiento.");
    }

    void Update()
    {
        if (!isGrabbed || interactorTransform == null || xrOrigin == null || headTransform == null)
            return;

        // 1. Calcular cuánto he girado el mando desde que lo cogí
        Quaternion delta = Quaternion.Inverse(startRotation) * interactorTransform.rotation;
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

        if (showDebug)
        {
            Debug.Log($"[Throttle] Axis({throttleAxis}): {axisAngle:F1}  Forward: {throttleForward:F2}  Brake: {brakeInput:F2}");
        }

        lastAngle = axisAngle;
        lastForwardInput = throttleForward;
        lastBrakeInput = brakeInput;

        // Actualizar velocidad actual
        if (throttleForward > 0f)
        {
            float target = maxSpeed * throttleForward;
            currentSpeed = Mathf.MoveTowards(currentSpeed, target, accelerationRate * Time.deltaTime);
        }
        else if (brakeInput > 0f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeRate * brakeInput * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, naturalDrag * Time.deltaTime);
        }

        if (currentSpeed <= 0.01f)
            return;

        // 2. Dirección = hacia donde mira la cabeza (solo en plano XZ)
        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            return;
        forward.Normalize();

        // 3. Aplicar movimiento al XR Origin
        Vector3 motion = forward * currentSpeed * Time.deltaTime;

        xrOrigin.position += motion;
    }
}
