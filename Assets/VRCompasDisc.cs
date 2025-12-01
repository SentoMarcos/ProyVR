using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VRCompassDisc : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Quest 3 HMD camera transform.")]
    public Transform headTransform;           // your VR camera
    public RectTransform compassDisc;         // this object's RectTransform
    public TextMeshProUGUI headingText;       // optional text readout

    private void Reset()
    {
        compassDisc = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (headTransform == null || compassDisc == null) return;

        // Get yaw of the head (looking left/right)
        float yaw = headTransform.eulerAngles.y;

        // Rotate the disc so that north on the disc always points to world north
        // Negative yaw so the disc appears to rotate under a fixed pointer
        compassDisc.localEulerAngles = new Vector3(-20f, 0f, -yaw);

        // Optional text: N / NE / E + angle
        if (headingText != null)
        {
            float heading = Mathf.Round(yaw);
            string dir = GetCardinalDirection(heading);
            headingText.text = $"{dir} {heading:000}°";
        }
    }

    string GetCardinalDirection(float yaw)
    {
        yaw = Mathf.Repeat(yaw, 360f);

        if (yaw >= 337.5f || yaw < 22.5f) return "N";
        if (yaw < 67.5f) return "NE";
        if (yaw < 112.5f) return "E";
        if (yaw < 157.5f) return "SE";
        if (yaw < 202.5f) return "S";
        if (yaw < 247.5f) return "SW";
        if (yaw < 292.5f) return "W";
        return "NW";
    }
}
