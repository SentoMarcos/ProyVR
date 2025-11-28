using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

public class GraphicSettingsManager : MonoBehaviour
{
    public UniversalRenderPipelineAsset urpAsset;
    public Volume postProcessingVolume;
    public TMP_Dropdown dropdownQuality;

    private Bloom bloom;
    private MotionBlur motionBlur;

    void Start()
    {
        dropdownQuality.ClearOptions();
        dropdownQuality.AddOptions(new List<string>() { "Low", "Medium", "High", "Ultra" });
        if (postProcessingVolume != null)
        {
            postProcessingVolume.profile.TryGet(out bloom);
            postProcessingVolume.profile.TryGet(out motionBlur);
        }
    }

    // ---------------------------
    // RENDER SCALE (VR clarity)
    // ---------------------------
    public void SetRenderScale(float value)
    {
        urpAsset.renderScale = value;
    }

    // ---------------------------
    // SHADOWS
    // ---------------------------
    public void SetShadows(int mode)
    {
        switch (mode)
        {
            case 0: // OFF
                urpAsset.shadowDistance = 0;
                urpAsset.mainLightShadowmapResolution = 256;
                break;

            case 1: // LOW
                urpAsset.shadowDistance = 10;
                urpAsset.mainLightShadowmapResolution = 512;
                break;

            case 2: // MEDIUM
                urpAsset.shadowDistance = 20;
                urpAsset.mainLightShadowmapResolution = 1024;
                break;

            case 3: // HIGH
                urpAsset.shadowDistance = 40;
                urpAsset.mainLightShadowmapResolution = 2048;
                break;
        }
    }


    // ---------------------------
    // ANTI ALIASING
    // ---------------------------
    public void SetAntiAliasing(int mode)
    {
        switch (mode)
        {
            case 0: urpAsset.msaaSampleCount = 1; break; // OFF
            case 1: urpAsset.msaaSampleCount = 2; break;
            case 2: urpAsset.msaaSampleCount = 4; break;
            case 3: urpAsset.msaaSampleCount = 8; break;
        }
    }

    // ---------------------------
    // BLOOM
    // ---------------------------
    public void SetBloom(bool enabled)
    {
        if (bloom != null)
            bloom.active = enabled;
    }

    // ---------------------------
    // MOTION BLUR
    // ---------------------------
    public void SetMotionBlur(bool enabled)
    {
        if (motionBlur != null)
            motionBlur.active = enabled;
    }

    // ---------------------------
    // GLOBAL QUALITY PRESET
    // ---------------------------
    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index, true);
        Debug.Log("Quality changed: " + index);
    }
}
