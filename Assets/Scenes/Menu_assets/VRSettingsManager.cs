using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using UnityEngine.UI;

public class VRSettingsManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioMixer masterMixer;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    private float musicBase = 1f;
    private float sfxBase = 1f;

    [Header("Locomotion")]
    public float moveSpeed = 1f;
    public bool invertYAxis = false;

    [Header("Graphics")]
    public float renderScale = 1f;

    void Start()
    {
        // Asignar callbacks de sliders VR
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        LoadAudioPrefs();
    }

    // ---------------- Audio ----------------
    public void SetMasterVolume(float value)
    {
        masterMixer.SetFloat("Master", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);

        // Aplicar master a Music y SFX
        ApplyMusicVolume(value * musicBase);
        ApplySFXVolume(value * sfxBase);

        // Actualizar sliders sin disparar eventos
        musicSlider.SetValueWithoutNotify(value * musicBase);
        sfxSlider.SetValueWithoutNotify(value * sfxBase);
    }

    public void SetMusicVolume(float value)
    {
        musicBase = value;
        ApplyMusicVolume(masterSlider.value * musicBase);
    }

    public void SetSFXVolume(float value)
    {
        sfxBase = value;
        ApplySFXVolume(masterSlider.value * sfxBase);
    }

    private void ApplyMusicVolume(float value)
    {
        masterMixer.SetFloat("Music", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
    }

    private void ApplySFXVolume(float value)
    {
        masterMixer.SetFloat("SFX", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
    }

    private void LoadAudioPrefs()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicBase = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxBase = PlayerPrefs.GetFloat("SFXVolume", 1f);

        masterSlider.value = master;
        musicSlider.value = master * musicBase;
        sfxSlider.value = master * sfxBase;

        SetMasterVolume(master);
    }

    public void SaveAudioPrefs()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicBase);
        PlayerPrefs.SetFloat("SFXVolume", sfxBase);
        PlayerPrefs.Save();
    }

    // ---------------- Controles ----------------
    public void ToggleInvertY(bool isInverted)
    {
        invertYAxis = isInverted;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
        var xrOrigin = Object.FindAnyObjectByType<XROrigin>();
        if (xrOrigin != null)
        {
            // Aplica speed si tu locomotion lo requiere
        }
    }

    // ---------------- Gráficos ----------------
    public void SetRenderScale(float scale)
    {
        renderScale = scale;
        XRSettings.eyeTextureResolutionScale = renderScale;
    }
}
