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
    public bool useAbsoluteTilt = true;

    [Header("Debug")]
    public bool showDebug = true;

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

        float throttleAngle = useAbsoluteTilt ? Mathf.Abs(axisAngle) : axisAngle;
        throttleAngle = Mathf.Max(0f, throttleAngle - deadZoneAngle);
        float throttle01 = Mathf.InverseLerp(0f, maxThrottleAngle, throttleAngle);
        throttle01 = Mathf.Clamp01(throttle01);

        if (showDebug)
        {
            Debug.Log($"[Throttle] Axis({throttleAxis}): {axisAngle:F1}  Throttle01: {throttle01:F2}");
        }

        // Si no hay casi gas, no nos movemos
        if (throttle01 <= 0.01f)
            return;

        // 2. Dirección = hacia donde mira la cabeza (solo en plano XZ)
        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            return;
        forward.Normalize();

        // 3. Aplicar movimiento al XR Origin
        float speed = maxSpeed * throttle01;
        Vector3 motion = forward * speed * Time.deltaTime;

        xrOrigin.position += motion;
    }
}
