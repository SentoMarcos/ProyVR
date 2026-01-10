using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class ControlMoto : MonoBehaviour
{
    public XRKnob acelerador;
    public Transform camaraVR;
    public Transform modeloVisual; // Arrastra aquí el objeto "DEF-Body"
    
    public float velocidadMaxima = 10f;
    
    [Header("Configuración de Giro")]
    public float sensibilidadGiro = 40f;   
    public float maximaInclinacionVisual = 30f; 
    public float suavizadoInclinacion = 5f;

    private float inclinacionActual = 0f;

    void Update()
    {
        float potencia = acelerador.value;

        if (potencia > 0.05f)
        {
            // 1. MOVIMIENTO LÓGICO (El padre avanza y gira sobre el suelo)
            transform.Translate(-Vector3.up * potencia * velocidadMaxima * Time.deltaTime);
            
            float inclinacionCabeza = camaraVR.localEulerAngles.z;
            if (inclinacionCabeza > 180) inclinacionCabeza -= 360;

            float factorGiro = -inclinacionCabeza / 45f; 
            factorGiro = Mathf.Clamp(factorGiro, -1f, 1f);

            // Giro horizontal (sobre el eje Z del padre debido a la rotación -90)
            transform.Rotate(0, 0, factorGiro * sensibilidadGiro * Time.deltaTime);

            // 2. INCLINACIÓN VISUAL (Solo al objeto visual)
            float inclinacionDeseada = factorGiro * maximaInclinacionVisual;
            inclinacionActual = Mathf.Lerp(inclinacionActual, inclinacionDeseada, Time.deltaTime * suavizadoInclinacion);

            if (modeloVisual != null)
            {
                // Aplicamos la inclinación lateral al modelo visual
                // En tu modelo, al ser hijo de un objeto a -90, el eje de "tumbado" suele ser el Y local
                modeloVisual.localEulerAngles = new Vector3(0f, inclinacionActual, 0f);
            }
        }
        else
        {
            // Enderezar suavemente al frenar
            inclinacionActual = Mathf.Lerp(inclinacionActual, 0, Time.deltaTime * suavizadoInclinacion);
            if (modeloVisual != null)
                modeloVisual.localEulerAngles = new Vector3(0f, inclinacionActual, 0f);
        }
    }
}