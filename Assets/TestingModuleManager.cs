using UnityEngine;

public class TestingModeManagerFixed : MonoBehaviour
{
    [Header("Configuración Modo Testing")]
    public bool enableTestingMode = true;
    public bool autoDetectPlatform = true;

    [Header("Componentes Testing")]
    public GameObject pcCameraRig;  // Arrastra PC_CameraRig aquí

    [Header("Componentes VR")]
    public GameObject vrRig;

    private bool isTestingModeActive = false;

    void Start()
    {
        // Primero desactivar todo
        if (pcCameraRig != null) pcCameraRig.SetActive(false);
        if (vrRig != null) vrRig.SetActive(false);

        DetermineMode();
    }

    void DetermineMode()
    {
        bool useTestingMode = enableTestingMode;

        // En editor, usar lo configurado
#if UNITY_EDITOR
        useTestingMode = enableTestingMode;
        Debug.Log($"Editor: Usando modo testing configurado: {useTestingMode}");
#else
        if (autoDetectPlatform)
        {
            try
            {
                bool isVR = UnityEngine.XR.XRSettings.enabled;
                useTestingMode = !isVR;
                Debug.Log($"Build: VR detectado: {isVR}, Modo testing: {useTestingMode}");
            }
            catch
            {
                useTestingMode = enableTestingMode;
                Debug.Log($"Build: Error detectando VR, usando configuración manual: {useTestingMode}");
            }
        }
#endif

        ApplyMode(useTestingMode);
    }

    void ApplyMode(bool testingMode)
    {
        isTestingModeActive = testingMode;

        // Activar/Desactivar PC
        if (pcCameraRig != null)
        {
            pcCameraRig.SetActive(testingMode);

            // Asegurar que la cámara sea MainCamera
            if (testingMode)
            {
                Camera cam = pcCameraRig.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    cam.tag = "MainCamera";
                    Debug.Log($"Cámara principal: {cam.name}");
                }
            }
        }

        // Activar/Desactivar VR
        if (vrRig != null)
        {
            vrRig.SetActive(!testingMode);
        }

        Debug.Log($"=== MODO {(testingMode ? "PC TESTING" : "VR")} ACTIVADO ===");
        Debug.Log($"PC Camera Rig: {(pcCameraRig != null ? pcCameraRig.activeSelf.ToString() : "null")}");
        Debug.Log($"VR Rig: {(vrRig != null ? vrRig.activeSelf.ToString() : "null")}");
    }

    void Update()
    {
        // Hotkey para alternar modo en Editor
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ToggleMode();
        }
#endif
    }

    public void EnableTestingMode() => ApplyMode(true);
    public void DisableTestingMode() => ApplyMode(false);
    public void ToggleMode() => ApplyMode(!isTestingModeActive);
}