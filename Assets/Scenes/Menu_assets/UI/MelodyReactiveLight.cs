using UnityEngine;

public class MelodyReactiveLight : MonoBehaviour
{
    public AudioSource audioSource;
    public float sensitivity = 10f;      // Para las frecuencias medias (melodía)
    public float threshold = 0.5f;       // Umbral para “detectar melodía”
    public int melodyStartSample = 50;   // Inicio del rango que consideramos “melodía”
    public int melodyEndSample = 150;    // Final del rango para melodía
    public Light[] sceneLights;             // Luz real que iluminará la escena
    public float maxIntensity = 5f;
    public Color lightColor = Color.cyan;

    private float[] samples = new float[512]; // Usa más muestras para más resolución

    void Start()
    {
        foreach (Light l in sceneLights)
        {
            l.color = lightColor;
            l.intensity = 0f;
        }
    }

    void Update()
    {
        if (audioSource == null || sceneLights.Length == 0) return;

        audioSource.GetSpectrumData(samples, 0, FFTWindow.Blackman);

        // Sumar la energía en el rango de la melodía
        float melodyIntensity = 0f;
        for (int i = melodyStartSample; i < melodyEndSample && i < samples.Length; i++)
        {
            melodyIntensity += samples[i];
        }

        melodyIntensity *= sensitivity;

        // Comparar con el umbral
        float targetIntensity = (melodyIntensity > threshold) ? maxIntensity : 0f;

        // Suavizar la intensidad y aplicarla a todas las luces
        foreach (Light l in sceneLights)
        {
            l.intensity = Mathf.Lerp(l.intensity, targetIntensity, Time.deltaTime * 5f);
        }
    }
}
