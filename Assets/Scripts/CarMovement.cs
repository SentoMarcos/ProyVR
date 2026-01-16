using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarMovement : MonoBehaviour
{
    public float speed = 8f;
    public Vector3 direction = Vector3.forward;

    public float stopThreshold = 0.02f;   // movimiento mínimo
    public float stopTime = 0.3f;          // tiempo parado

    private Rigidbody rb;
    private Vector3 lastPosition;
    private float stoppedTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = //RigidbodyConstraints.FreezeRotationX
                       RigidbodyConstraints.FreezeRotationZ;

        lastPosition = rb.position;
    }

    void FixedUpdate()
    {
        Vector3 move = direction.normalized * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);

        DetectStop();
        CheckFall();

        // Ajuste de rotación según pendiente
        UpdateRotationOnSlope();
    }

    void DetectStop()
    {
        float distanceMoved = Vector3.Distance(rb.position, lastPosition);

        if (distanceMoved < stopThreshold)
        {
            stoppedTimer += Time.fixedDeltaTime;

            if (stoppedTimer >= stopTime)
            {
                Destroy(gameObject); // 💥 coche eliminado
            }
        }
        else
        {
            stoppedTimer = 0f;
        }

        lastPosition = rb.position;
    }

    void CheckFall()
    {
        if (rb.position.y < -5f) // ejemplo: fuera del mundo
        {
            Destroy(gameObject);
        }
    }

    void UpdateRotationOnSlope()
    {
        Ray ray = new Ray(rb.position + Vector3.up, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 2f))
        {
            Vector3 groundNormal = hit.normal;

            // Mantener dirección forward del coche
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(forward, groundNormal);

            rb.MoveRotation(
                Quaternion.Slerp(rb.rotation, targetRotation, 10f * Time.fixedDeltaTime)
            );
        }
    }



}