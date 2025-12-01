using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AccessibilityManager : MonoBehaviour
{
    // CONFORT VISUAL
    public Toggle vignetteToggle;
    public Slider vignetteStrength;
    public Toggle tunnelVisionToggle;
    public Toggle reduceHeadBobToggle;

    // COLOR
    public TMP_Dropdown colorblindDropdown;

    // SUBTÍTULOS
    public Toggle subtitlesToggle;
    public Slider subtitlesSize;
    public Toggle subtitlesBackgroundToggle;

    // AUDIO ACCESIBLE
    public Toggle monoAudioToggle;
    public Toggle reduceLoudSoundsToggle;

    // INTERACCIÓN
    public Toggle bigUIModeToggle;
    public Toggle autoHoverSelectToggle;

    // POSTPROCESSING & SISTEMAS
    public UnityEngine.Rendering.Volume postFXVolume;

    private UnityEngine.Rendering.Universal.Vignette vignette;

    void Start()
    {
        if (postFXVolume != null)
        {
            postFXVolume.profile.TryGet(out vignette);
        }
    }

    // --------------------------
    // CONFORT VISUAL
    // --------------------------

    public void SetVignette(bool enabled)
    {
        if (vignette != null)
        {
            vignette.active = enabled;
        }
    }

    public void SetVignetteStrength(float value)
    {
        if (vignette != null)
        {
            vignette.intensity.Override(value);
        }
    }

    public void SetTunnelVision(bool enabled)
    {
        // Aquí puedes poner tu propio sistema de Tunnel Vision
        Debug.Log("Tunnel vision: " + enabled);
    }

    public void SetHeadBob(bool enabled)
    {
        Debug.Log("Reduce Head Bob: " + enabled);
    }

    // --------------------------
    // COLOR / DALTONISMO
    // --------------------------

    public void SetColorblindMode(int index)
    {
        // Aquí cambiarías tu shader global
        // o activarías un material overlay

        string mode = "";
        switch (index)
        {
            case 0: mode = "None"; break;
            case 1: mode = "Protanopia"; break;
            case 2: mode = "Deuteranopia"; break;
            case 3: mode = "Tritanopia"; break;
        }

        Debug.Log("Colorblind mode: " + mode);
    }

    // --------------------------
    // SUBTÍTULOS
    // --------------------------

    public void SetSubtitles(bool enabled)
    {
        Debug.Log("Subtitles: " + enabled);
    }

    public void SetSubtitleSize(float value)
    {
        Debug.Log("Subtitle size: " + value);
    }

    public void SetSubtitleBackground(bool enabled)
    {
        Debug.Log("Subtitle background: " + enabled);
    }

    // --------------------------
    // AUDIO ACCESIBLE
    // --------------------------

    public void SetMonoAudio(bool enabled)
    {
        AudioSettings.speakerMode = enabled ?
            AudioSpeakerMode.Mono :
            AudioSpeakerMode.Stereo;

        Debug.Log("Mono audio: " + enabled);
    }

    public void SetReduceLoudSounds(bool enabled)
    {
        Debug.Log("Reduce loud sounds: " + enabled);
    }

    // --------------------------
    // INTERACCIÓN ACCESIBLE
    // --------------------------

    public void SetBigUIMode(bool enabled)
    {
        // Aquí puedes escalar la UI mediante CanvasScaler
        Debug.Log("Big UI Mode: " + enabled);
    }

    public void SetAutoHoverSelect(bool enabled)
    {
        Debug.Log("Auto Hover Select: " + enabled);
    }
}
