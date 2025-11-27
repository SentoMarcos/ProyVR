using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeManager : MonoBehaviour
{
    [Header("Audio Mixer Reference")]
    public AudioMixer masterMixer;

    [Header("UI Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    private const string MASTER = "MasterVolume";
    private const string MUSIC = "MusicVolume";
    private const string SFX = "SFXVolume";

    private float musicBase = 1f;
    private float sfxBase = 1f;

    void Start()
    {
        // Asignar callbacks
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        LoadVolumePrefs();
    }

    public void SetMasterVolume(float value)
    {
        masterMixer.SetFloat(MASTER, Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);

        // Aplicar master a Music y SFX
        ApplyMusicVolume(value * musicBase);
        ApplySFXVolume(value * sfxBase);

        // Actualizar sliders visualmente
        musicSlider.SetValueWithoutNotify(value * musicBase);
        sfxSlider.SetValueWithoutNotify(value * sfxBase);
    }

    public void SetMusicVolume(float value)
    {
        musicBase = value;

        float masterValue = masterSlider.value;
        ApplyMusicVolume(masterValue * musicBase);
    }

    public void SetSFXVolume(float value)
    {
        sfxBase = value;

        float masterValue = masterSlider.value;
        ApplySFXVolume(masterValue * sfxBase);
    }

    private void ApplyMusicVolume(float value)
    {
        masterMixer.SetFloat(MUSIC, Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
    }

    private void ApplySFXVolume(float value)
    {
        masterMixer.SetFloat(SFX, Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
    }

    public void LoadVolumePrefs()
    {
        float master = PlayerPrefs.GetFloat(MASTER, 1f);
        musicBase = PlayerPrefs.GetFloat(MUSIC, 1f);
        sfxBase = PlayerPrefs.GetFloat(SFX, 1f);

        // Ajustar sliders
        masterSlider.value = master;
        musicSlider.value = master * musicBase;
        sfxSlider.value = master * sfxBase;

        // Aplicar volúmenes
        SetMasterVolume(master);
    }

    public void SaveVolumePrefs()
    {
        PlayerPrefs.SetFloat(MASTER, masterSlider.value);
        PlayerPrefs.SetFloat(MUSIC, musicBase);
        PlayerPrefs.SetFloat(SFX, sfxBase);
        PlayerPrefs.Save();
    }
}
