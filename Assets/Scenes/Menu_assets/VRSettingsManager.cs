using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Audio;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

public class VRSettingsManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioMixer masterMixer;

    [Header("Locomotion")]
    public float moveSpeed = 1f;
    public bool invertYAxis = false;

    [Header("Graphics")]
    public float renderScale = 1f; // XR Render Scale

    // -------------------------------
    // AUDIO
    // -------------------------------

    // MASTER (0.0001f a 1)
    public void SetMasterVolume(float value)
    {
        masterMixer.SetFloat("Master", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
    }

    // MUSIC (0.0001f a 1)
    public void SetMusicVolume(float value)
    {
        masterMixer.SetFloat("Music", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
    }

    // SFX (0.0001f a 1)
    public void SetSFXVolume(float value)
    {
        masterMixer.SetFloat("SFX", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
    }

    // -------------------------------
    // CONTROLES VARIOS
    // -------------------------------

    // Invertir eje Y
    public void ToggleInvertY(bool isInverted)
    {
        invertYAxis = isInverted;
    }

    // Ajustar velocidad de movimiento
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;

        var xrOrigin = Object.FindAnyObjectByType<XROrigin>();
        if (xrOrigin != null)
        {
            // Aquí puedes aplicar el nuevo speed si usas locomotion personalizada
        }
    }

    // Ajustar render scale
    public void SetRenderScale(float scale)
    {
        renderScale = scale;
        XRSettings.eyeTextureResolutionScale = renderScale;
    }
}
