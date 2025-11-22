using UnityEngine;

public class MusicReactiveLights : MonoBehaviour
{
    public AudioSource audioSource;
    public float sensitivity = 5f;       // Escala la intensidad del bajo
    public float threshold = 0.1f;       // Umbral para encender la luz
    public int bassSamples = 16;         // Número de muestras bajas
    public Color baseColor = Color.cyan;
    public Light[] ledLights;            // Luces a lo largo del LED
    public float maxIntensity = 5f;      // Intensidad máxima de las luces

    private Renderer rend;
    private Material mat;
    private float[] samples = new float[256];

    void Start()
    {
        rend = GetComponent<Renderer>();
        mat = rend.material;
        mat.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        if (audioSource == null || ledLights.Length == 0) return;

        audioSource.GetSpectrumData(samples, 0, FFTWindow.Blackman);

        // Calcular intensidad de bajos
        float bassIntensity = 0f;
        for (int i = 0; i < bassSamples; i++)
            bassIntensity += samples[i];

        bassIntensity *= sensitivity;

        // Encender o apagar emisión del material
        if (bassIntensity > threshold)
            mat.SetColor("_EmissionColor", baseColor * 5f);
        else
            mat.SetColor("_EmissionColor", Color.black);

        // Encender o apagar luces reales suavemente
        float targetIntensity = bassIntensity > threshold ? maxIntensity : 0f;
        foreach (Light l in ledLights)
        {
            l.intensity = Mathf.Lerp(l.intensity, targetIntensity, Time.deltaTime * 10f);
            l.color = baseColor;
        }
    }
}
