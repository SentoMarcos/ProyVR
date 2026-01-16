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
    public GameObject[] objectsToActivate; // Asigna DEF-Body, DEF-Wheel.Bk, Root
    public GameObject truckRootToDisable; // Asigna aquí el GO raíz del camión
    public Rigidbody[] rigidbodiesToEnableGravity; // Activa gravedad al pulsar


    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("CLICK DETECTADO SIN ONCLICK");

        // Ocultar UI
        if (uiContainer != null)
            uiContainer.SetActive(false);
        else
            Debug.Log("UIContainer ES NULL");

        // Desactivar camión completo
        if (truckRootToDisable != null)
            truckRootToDisable.SetActive(false);
        else
            Debug.Log("truckRootToDisable ES NULL");

        // Activar objetos solicitados
        if (objectsToActivate != null && objectsToActivate.Length > 0)
        {
            foreach (var obj in objectsToActivate)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }
        else
        {
            Debug.Log("No hay objetos asignados para activar");
        }

        // Activar gravedad en los rigidbodies asignados
        if (rigidbodiesToEnableGravity != null && rigidbodiesToEnableGravity.Length > 0)
        {
            foreach (var rb in rigidbodiesToEnableGravity)
            {
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
            }
        }
        else
        {
            Debug.Log("No hay rigidbodies asignados para activar gravedad");
        }

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
        // 1️⃣ Subir escena antigua 1 unidad
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            root.transform.position += new Vector3(0f, 1f, 0f);
        }

        // 2️⃣ Desaparecer objeto específico de la escena antigua
        GameObject oldObj = GameObject.Find("OldObject");
        if (oldObj != null)
            Destroy(oldObj); // o oldObj.SetActive(false);

        // 3️⃣ Ajustar escena nueva como antes
        float targetY = 0.72f;
        float planeZ = 0f;
        float distanceForward = 0.5f;
        float planeLength = groundPlane.localScale.z;

        float yOffset = targetY - groundPlane.position.y;

        foreach (var root in scene.GetRootGameObjects())
        {
            root.transform.position += new Vector3(0f, yOffset, 0f);
        }

        // 4️⃣ Ajustar Z de la carretera
        float minRoadZ = float.MaxValue;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.transform)
            {
                if (child != null && child.CompareTag("Road")) // si quieres filtrar solo carreteras
                {
                    if (child.position.z < minRoadZ) minRoadZ = child.position.z;
                }
            }
        }

        float segmentLength = 20f;
        float roadOffsetZ = planeZ + (planeLength / 2f) + distanceForward - minRoadZ - segmentLength;

        foreach (var root in scene.GetRootGameObjects())
        {
            root.transform.position += new Vector3(0f, 0f, roadOffsetZ);
        }
    }

}