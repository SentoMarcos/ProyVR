using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class customAgarre : MonoBehaviour
{
    XRBaseInteractable grab;
    public GameObject customRight;
    public GameObject customLeft;
    private void Awake()
    {
        grab = this.GetComponent<XRBaseInteractable>();
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnDeatach);
    }

    private void OnDeatach(SelectExitEventArgs args)
    {
        args?.interactorObject?.transform?.parent?.GetComponentInChildren<ControllerAnimator>(true)?.gameObject?.SetActive(true);
        showHand(false,args?.interactorObject?.transform?.parent);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        args?.interactorObject?.transform?.parent?.GetComponentInChildren<ControllerAnimator>(true)?.gameObject?.SetActive(false);
        showHand(true, args?.interactorObject?.transform?.parent);
    }

    private void showHand(bool bMostrar, Transform tr)
    {
        if (tr != null && tr.name.Contains("Left"))
        {
            customLeft.SetActive(bMostrar);
        }
        else if (tr != null && tr.name.Contains("Right"))
        {
            customRight.SetActive(bMostrar);
        }
    }
}
