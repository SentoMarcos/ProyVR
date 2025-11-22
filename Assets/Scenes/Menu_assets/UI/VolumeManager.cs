using UnityEngine;
using UnityEngine.Audio;

public class VolumeManager : MonoBehaviour
{
    [Header("Audio Mixer Reference")]
    public AudioMixer masterMixer;

    // Estos nombres deben coincidir con los parámetros expuestos en el Audio Mixer
    private const string MASTER = "MasterVolume";
    private const string MUSIC = "MusicVolume";
    private const string SFX = "SFXVolume";

    /// <summary>
    /// Ajusta el volumen del master
    /// </summary>
    /// <param name="value">Valor de 0 a 1 (slider)</param>
    public void SetMasterVolume(float value)
    {
        // Convertir a decibeles logarítmicamente
        masterMixer.SetFloat(MASTER, Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
    }

    /// <summary>
    /// Ajusta el volumen de la música
    /// </summary>
    public void SetMusicVolume(float value)
    {
        masterMixer.SetFloat(MUSIC, Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
    }

    /// <summary>
    /// Ajusta el volumen de los efectos
    /// </summary>
    public void SetSFXVolume(float value)
    {
        masterMixer.SetFloat(SFX, Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
    }

    /// <summary>
    /// Cargar valores guardados (opcional)
    /// </summary>
    public void LoadVolumePrefs()
    {
        SetMasterVolume(PlayerPrefs.GetFloat(MASTER, 1f));
        SetMusicVolume(PlayerPrefs.GetFloat(MUSIC, 1f));
        SetSFXVolume(PlayerPrefs.GetFloat(SFX, 1f));
    }

    /// <summary>
    /// Guardar valores actuales (opcional)
    /// </summary>
    public void SaveVolumePrefs()
    {
        float master, music, sfx;
        masterMixer.GetFloat(MASTER, out master);
        masterMixer.GetFloat(MUSIC, out music);
        masterMixer.GetFloat(SFX, out sfx);

        // Convertir de dB a valor 0-1
        PlayerPrefs.SetFloat(MASTER, Mathf.Pow(10, master / 20f));
        PlayerPrefs.SetFloat(MUSIC, Mathf.Pow(10, music / 20f));
        PlayerPrefs.SetFloat(SFX, Mathf.Pow(10, sfx / 20f));
        PlayerPrefs.Save();
    }
}
