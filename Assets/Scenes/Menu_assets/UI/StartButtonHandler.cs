using UnityEngine;
using UnityEngine.EventSystems;

public class StartButtonHandler : MonoBehaviour, IPointerClickHandler
{
    public GameObject uiContainer;     // UI a ocultar
    public Animator targetAnimator;    // Objeto con Animator
    public string triggerName = "StartGame"; // Trigger a ejecutar

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("CLICK DETECTADO SIN ONCLICK");

        // Ocultar UI
        if (uiContainer != null)
            uiContainer.SetActive(false);
        else
            Debug.Log("UIContainer ES NULL");

        // Ejecutar animación
        if (targetAnimator != null)
            targetAnimator.SetTrigger(triggerName);
        else
            Debug.Log("Animator ES NULL");
    }
}
