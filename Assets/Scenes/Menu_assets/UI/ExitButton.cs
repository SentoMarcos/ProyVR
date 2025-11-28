using UnityEngine;

public class ExitButton : MonoBehaviour
{
    // Método que se llama desde el botón
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
#if UNITY_EDITOR
        // Si estás en el editor, detiene el modo Play
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Si es build, cierra la aplicación
        Application.Quit();
#endif
    }
}
