using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Animator doorAnimator; // Arrastra aquí el Animator de la puerta

    // Método que ejecutará la animación
    public void OpenDoor()
    {
        doorAnimator.Play("Cube|CubeAction");
    }

    // O si usas un bool "Open" en el Animator:
    /*
    public void ToggleDoor()
    {
        doorAnimator.SetBool("Open", true); // Cambia a false si quieres cerrar
    }
    */
}
