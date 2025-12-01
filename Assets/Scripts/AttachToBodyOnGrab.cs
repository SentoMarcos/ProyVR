using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class AttachToBodyOnGrab : MonoBehaviour
{
    [Header("Dónde quieres que se pegue")]
    public Transform bodyParent; // Asigna Camera Offset o Main Camera en el Inspector

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private Transform originalParent;

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
        // Desactivar física
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Hacer hijo del cuerpo
        transform.SetParent(bodyParent, true);
        // Opcional: ajustar posición local respecto al cuerpo
        // transform.localPosition = new Vector3(0f, 0f, 0.3f);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // Volver al padre original
        transform.SetParent(originalParent, true);

        // Recuperar física
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }
}
