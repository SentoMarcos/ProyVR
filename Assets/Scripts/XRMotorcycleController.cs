using UnityEngine;
using UnityEngine.InputSystem;

/**
 * XRMotorcycleController (versión mínima): solo usa la rotación de un mando XR para avanzar en línea recta.
 * Sin curva de giro ni freno manual; rota la muñeca como un acelerador y el rigidbody se impulsa hacia delante.
 */
[RequireComponent(typeof(Rigidbody))]
public class XRMotorcycleController : MonoBehaviour
{
    public enum DriveAxis
    {
        Roll,
        Pitch,
        Yaw
    }

    [Header("XR Input")]
    [Tooltip("Acción existente que entrega la rotación del mando (mantén tu InputAction de RightHand).")]
    public InputActionProperty drivePoseAction;

    [Tooltip("Eje del mando que actuará como acelerador (roll = girar la muñeca).")]
    public DriveAxis driveAxis = DriveAxis.Roll;

    [Tooltip("Marca si necesitas invertir el sentido de la rotación.")]
    public bool invertAxis;

    [Tooltip("Si está activo, tomar la magnitud absoluta del giro (vale tanto hacia un lado como hacia el otro).")]
    public bool useAbsoluteTilt = true;

    [Header("Sensibilidad")]
    [Tooltip("Grados ignorados antes de empezar a acelerar.")]
    public float deadZoneDegrees = 5f;

    [Tooltip("Grados necesarios para alcanzar acelerador máximo.")]
    public float maxTiltDegrees = 40f;

    [Header("Movimiento")]
    [Tooltip("Velocidad máxima en m/s.")]
    public float maxSpeed = 8f;

    [Tooltip("Aceleración (m/s²) cuando hay input.")]
    public float acceleration = 12f;

    [Tooltip("Deceleración automática (m/s²) cuando sueltas el mando.")]
    public float releaseDeceleration = 15f;

    [Tooltip("Suavizado (en unidades por segundo) aplicado al valor del acelerador para evitar temblores.")]
    public float inputSmoothing = 10f;

    [Tooltip("Multiplicador extra de gravedad para que el vehículo se adhiera al suelo.")]
    public float gravityMultiplier = 1.2f;

    [Header("Debug (solo lectura)")]
    [SerializeField, Tooltip("Último ángulo leído del mando.")]
    private float lastAngle;

    [SerializeField, Tooltip("Último valor normalizado de acelerador (0-1).")]
    private float lastInput;

    private Rigidbody _rigidbody;
    private float _smoothedInput;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = true;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void OnEnable()
    {
        drivePoseAction.action?.Enable();
    }

    private void OnDisable()
    {
        drivePoseAction.action?.Disable();
    }

    private void FixedUpdate()
    {
        if (drivePoseAction.action == null)
        {
            return;
        }

        Quaternion controllerRotation = drivePoseAction.action.ReadValue<Quaternion>();
        float angle = ExtractAxisAngle(controllerRotation, driveAxis);
        if (invertAxis)
        {
            angle = -angle;
        }

        float throttle = EvaluateThrottle(angle, deadZoneDegrees, maxTiltDegrees);
        if (useAbsoluteTilt)
        {
            throttle = Mathf.Abs(throttle);
        }

        throttle = Mathf.Clamp01(throttle);

        if (inputSmoothing > 0f)
        {
            _smoothedInput = Mathf.MoveTowards(_smoothedInput, throttle, inputSmoothing * Time.fixedDeltaTime);
        }
        else
        {
            _smoothedInput = throttle;
        }

        lastAngle = angle;
        lastInput = _smoothedInput;

        Vector3 forward = transform.forward;
        float currentSpeed = Vector3.Dot(_rigidbody.linearVelocity, forward);
        float targetSpeed = maxSpeed * _smoothedInput;
        float speedError = targetSpeed - currentSpeed;

        if (_smoothedInput > 0.001f)
        {
            float accel = Mathf.Clamp(speedError, 0f, acceleration);
            _rigidbody.AddForce(forward * accel, ForceMode.Acceleration);
        }
        else if (currentSpeed > 0.01f)
        {
            float decel = Mathf.Min(currentSpeed, releaseDeceleration * Time.fixedDeltaTime);
            _rigidbody.AddForce(-forward * (decel / Time.fixedDeltaTime), ForceMode.Acceleration);
        }

        if (gravityMultiplier > 1f)
        {
            Vector3 extraGravity = Physics.gravity * (gravityMultiplier - 1f);
            _rigidbody.AddForce(extraGravity, ForceMode.Acceleration);
        }
    }

    private static float ExtractAxisAngle(in Quaternion rotation, DriveAxis axis)
    {
        Vector3 euler = rotation.eulerAngles;
        return axis switch
        {
            DriveAxis.Roll => -euler.z,
            DriveAxis.Pitch => euler.x,
            DriveAxis.Yaw => euler.y,
            _ => 0f
        };
    }

    private static float EvaluateThrottle(float rawAngle, float deadZone, float maxTilt)
    {
        float angle = NormalizeAngle(rawAngle);
        float magnitude = Mathf.Abs(angle);

        if (magnitude <= deadZone || maxTilt <= deadZone)
        {
            return 0f;
        }

        float normalized = Mathf.Clamp01((magnitude - deadZone) / (maxTilt - deadZone));
        return normalized * Mathf.Sign(angle);
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
        {
            angle -= 360f;
        }
        else if (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }
}
