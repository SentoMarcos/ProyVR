using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class StartButtonHandler : MonoBehaviour, IPointerClickHandler
{
    public GameObject uiContainer;     // UI a ocultar
    public Animator targetAnimator;    // Objeto con Animator
    public string triggerName = "StartGame"; // Trigger a ejecutar
    public string sceneToLoad = "Rogers"; // ← Añade aquí tu escena
    public Transform truck; // Asigna el camión aquí
    public Transform groundPlane; // Asigna el plane del camión aquí
    public float distanceForward = -50f; // distancia donde aparecerá la otra escena


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

        SceneManager.sceneLoaded += OnSceneLoaded;

        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        float targetY = 0.72f;
        float planeZ = 0f; // aquí decides dónde quieres el plane en Z
        float distanceForward = 0.5f; // espacio entre plane y carretera
        float planeLength = groundPlane.localScale.z; // longitud del plane

        // 1️⃣ Subir toda la escena del camión
        float yOffset = targetY - groundPlane.position.y;
        float zOffset = planeZ - groundPlane.position.z;

        Vector3 camSceneOffset = new Vector3(0f, yOffset, zOffset);
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            root.transform.position += camSceneOffset;
        }

        // 2️⃣ Ajustar carretera procedural
        float minRoadZ = float.MaxValue;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.scene == scene)
            {
                foreach (Transform child in root.transform)
                {
                    if (child != null)
                    {
                        float z = child.position.z;
                        if (z < minRoadZ) minRoadZ = z;
                    }
                }
            }
        }

        // Offset para que el primer segmento quede justo delante del plane
        float roadOffsetZ = planeZ + (planeLength / 2f) + distanceForward - minRoadZ;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.scene == scene)
            {
                foreach (Transform child in root.transform)
                {
                    if (child != null)
                    {
                        child.position += new Vector3(0f, 0f, roadOffsetZ);
                    }
                }
            }
        }
    }
}
