using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Video;

/// <summary>
/// Reproduce un video 360 cuando se pulsa el botón asociado.
/// Crea un VideoPlayer, genera un RenderTexture y lo aplica a un skybox panorámico.
/// </summary>
public class TrailerButtonHandler : MonoBehaviour, IPointerClickHandler
{
    [Header("Video")]
    [Tooltip("Clip del tráiler. Si es null se usará la URL.")]
    public VideoClip trailerClip;
    [Tooltip("URL de streaming (YouTube no es compatible, usa archivos mp4/ogv expuestos por HTTP).")]
    public string streamingUrl;
    public bool loopVideo = true;

    [Header("Skybox 360")]
    [Tooltip("Plantilla de skybox panorámico; si es null se creará una al vuelo (Shader 'Skybox/Panoramic').")]
    public Material skyboxTemplate;
    public float skyboxExposure = 1.1f;
    public int renderTextureWidth = 2048;
    public int renderTextureHeight = 1024;

    [Header("Audio")]
    public bool playAudio = true;
    public AudioSource customAudioSource;

    [Header("UI opcional")]
    [Tooltip("Raíz de la UI a afectar (por ejemplo el Panel). Puedes arrastrar el GameObject desde la jerarquía.")]
    public GameObject uiRootToHide;
    public bool hideUiOnClick = true;

    [Header("Mundo 360")]
    [Tooltip("Objetos que se ocultarán mientras el tráiler esté reproduciéndose (por ejemplo, el camión/interior).")]
    public GameObject[] worldObjectsToHide;
    public bool hideWorldOnPlay = true;

    private VideoPlayer videoPlayer;
    private RenderTexture videoTexture;
    private Material runtimeSkybox;
    private Material previousSkybox;
    private Coroutine prepareRoutine;
    private AudioSource internalAudioSource;
    private UnityEngine.UI.Button cachedButton;
    private readonly System.Collections.Generic.List<(GameObject go, bool wasActive)> hiddenObjects = new();
    // UI-hide helpers: track images/buttons we disable so we can restore them
    private List<Image> disabledButtonImages = new List<Image>();
    private List<Button> disabledButtons = new List<Button>();

    public void OnPointerClick(PointerEventData eventData)
    {
        TryPlayTrailer();
    }

    private void Awake()
    {
        cachedButton = GetComponent<UnityEngine.UI.Button>();
        if (cachedButton != null)
        {
            cachedButton.onClick.RemoveListener(TryPlayTrailer);
            cachedButton.onClick.AddListener(TryPlayTrailer);
        }
    }

    private void OnEnable()
    {
        if (cachedButton != null)
        {
            cachedButton.onClick.RemoveListener(TryPlayTrailer);
            cachedButton.onClick.AddListener(TryPlayTrailer);
        }
    }

    private void OnDisable()
    {
        if (cachedButton != null)
        {
            cachedButton.onClick.RemoveListener(TryPlayTrailer);
        }
    }

    public void TryPlayTrailer()
    {
        Debug.Log("[TrailerButtonHandler] Play Trailer solicitado.");
        if (trailerClip == null && string.IsNullOrWhiteSpace(streamingUrl))
        {
            Debug.LogWarning("[TrailerButtonHandler] No hay VideoClip ni URL asignada.");
            return;
        }

        SetupVideoPlayer();
        LoadSource();

        if (prepareRoutine != null)
        {
            StopCoroutine(prepareRoutine);
        }
        prepareRoutine = StartCoroutine(PrepareAndPlay());

        // Ahora ocultar UI y mundo DESPUÉS de iniciar la coroutine
        if (hideUiOnClick && uiRootToHide != null)
        {
            // Instead of disabling the whole CanvasGroup, disable only the Image component of Buttons
            disabledButtonImages.Clear();
            disabledButtons.Clear();
            var buttons = uiRootToHide.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (b == null)
                    continue;
                var img = b.GetComponent<Image>();
                if (img != null && img.enabled)
                {
                    disabledButtonImages.Add(img);
                    img.enabled = false;
                }
                if (b.interactable)
                {
                    disabledButtons.Add(b);
                    b.interactable = false;
                }
            }
        }

        if (hideWorldOnPlay)
        {
            HideWorldObjects();
        }
    }

    private void SetupVideoPlayer()
    {
        if (videoPlayer != null)
            return;

        GameObject holder = new GameObject("TrailerVideoPlayer");
        holder.transform.SetParent(transform, false);
        holder.hideFlags = HideFlags.DontSaveInBuild;

        videoPlayer = holder.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = loopVideo;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

        if (playAudio)
        {
            AudioSource audioSource = customAudioSource;
            if (audioSource == null)
            {
                audioSource = holder.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
                internalAudioSource = audioSource;
            }
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, audioSource);
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }

        videoTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 0)
        {
            wrapMode = TextureWrapMode.Clamp,
            name = "Trailer360_RT"
        };
        videoPlayer.targetTexture = videoTexture;

        if (skyboxTemplate != null)
        {
            runtimeSkybox = Instantiate(skyboxTemplate);
        }
        else
        {
            runtimeSkybox = new Material(Shader.Find("Skybox/Panoramic"));
        }
        runtimeSkybox.SetTexture("_Tex", videoTexture);
        runtimeSkybox.SetFloat("_Exposure", skyboxExposure);
        runtimeSkybox.SetFloat("_Rotation", 0f);
        // Forzar mapeo 360º
        if (runtimeSkybox.HasProperty("_Mapping"))
        {
            runtimeSkybox.SetFloat("_Mapping", 1f); // 1 = Latitude-Longitude Layout
        }

        Debug.Log($"[TrailerButtonHandler] RenderTexture: {videoTexture.width}x{videoTexture.height}, isCreated={videoTexture.IsCreated()}.");
        Debug.Log($"[TrailerButtonHandler] Skybox shader: {runtimeSkybox.shader.name}, Mapping: {(runtimeSkybox.HasProperty("_Mapping") ? runtimeSkybox.GetFloat("_Mapping").ToString() : "none")}");
    }

    private void LoadSource()
    {
        videoPlayer.Stop();
        videoPlayer.isLooping = loopVideo;

        if (trailerClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = trailerClip;
        }
        else
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = streamingUrl;
        }
    }

    private IEnumerator PrepareAndPlay()
    {
        Debug.Log("[TrailerButtonHandler] Preparando video...");
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        previousSkybox = RenderSettings.skybox;
        RenderSettings.skybox = runtimeSkybox;
        DynamicGI.UpdateEnvironment();

        videoPlayer.Play();
        Debug.Log("[TrailerButtonHandler] Video en reproducción.");
    }

    public void StopTrailer()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.Stop();
        if (internalAudioSource != null)
        {
            internalAudioSource.Stop();
        }
        if (previousSkybox != null)
        {
            RenderSettings.skybox = previousSkybox;
            DynamicGI.UpdateEnvironment();
        }

        if (hideWorldOnPlay)
        {
            RestoreWorldObjects();
        }
        // Restaurar imágenes e interactividad de botones si los desactivamos
        if (hideUiOnClick && uiRootToHide != null)
        {
            foreach (var img in disabledButtonImages)
            {
                if (img != null)
                    img.enabled = true;
            }
            disabledButtonImages.Clear();
            foreach (var b in disabledButtons)
            {
                if (b != null)
                    b.interactable = true;
            }
            disabledButtons.Clear();
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
        if (videoTexture != null)
        {
            videoTexture.Release();
        }
        if (hideWorldOnPlay)
        {
            RestoreWorldObjects();
        }
    }

    private void HideWorldObjects()
    {
        hiddenObjects.Clear();
        if (worldObjectsToHide == null)
            return;

        foreach (var obj in worldObjectsToHide)
        {
            if (obj == null)
                continue;

            hiddenObjects.Add((obj, obj.activeSelf));
            obj.SetActive(false);
        }
    }

    private void RestoreWorldObjects()
    {
        if (hiddenObjects.Count == 0)
            return;

        foreach (var entry in hiddenObjects)
        {
            if (entry.go == null)
                continue;

            entry.go.SetActive(entry.wasActive);
        }
        hiddenObjects.Clear();
    }
}
