using UnityEngine;
using UnityEngine.InputSystem; // Necesario para el gatillo
using UnityEngine.XR.Content.Interaction;

public class ControlMoto : MonoBehaviour
{
    [Header("Controles")]
    public XRKnob acelerador; // Puño derecho
    public InputActionProperty frenoAction; // Gatillo izquierdo
    public Transform camaraVR;
    public Transform modeloVisual; 
    
    [Header("Ajustes de Movimiento")]
    public float velocidadMaxima = 15f;
    public float fuerzaFrenado = 8f; // El freno de gatillo suele ser más seco
    
    [Header("Ajustes de Giro")]
    public float sensibilidadGiro = 40f;   
    public float maximaInclinacionVisual = 25f; 
    public float suavizadoInclinacion = 5f;

    private float velocidadActual = 0f;
    private float inclinacionActual = 0f;

    void Update()
    {
        // 1. LEER INPUTS
        float valorAcelerador = acelerador != null ? acelerador.value : 0f;
        
        // Leemos el valor del gatillo (va de 0.0 a 1.0)
        float valorFreno = frenoAction.action.ReadValue<float>();

        // 2. LÓGICA DE VELOCIDAD
        if (valorFreno > 0.1f) // Prioridad al freno
        {
            // Frenado progresivo según cuánto aprietes el gatillo
            velocidadActual = Mathf.Lerp(velocidadActual, 0, Time.deltaTime * fuerzaFrenado * valorFreno);
        }
        else if (valorAcelerador > 0.05f)
        {
            // Aceleración normal
            velocidadActual = Mathf.Lerp(velocidadActual, valorAcelerador * velocidadMaxima, Time.deltaTime);
        }
        else
        {
            // Rozamiento natural (se para sola poco a poco)
            velocidadActual = Mathf.Lerp(velocidadActual, 0, Time.deltaTime * 0.5f);
        }

        // 3. MOVIMIENTO Y GIRO (Tu lógica corregida)
        if (velocidadActual > 0.1f)
        {
            transform.Translate(-Vector3.up * velocidadActual * Time.deltaTime);
            
            float zRot = camaraVR.localEulerAngles.z;
            if (zRot > 180) zRot -= 360;

            float factorGiro = -Mathf.Clamp(zRot / 30f, -1f, 1f);
            transform.Rotate(0, 0, factorGiro * sensibilidadGiro * Time.deltaTime);

            // 4. INCLINACIÓN VISUAL
            float inclinacionDeseada = factorGiro * maximaInclinacionVisual;
            inclinacionActual = Mathf.Lerp(inclinacionActual, inclinacionDeseada, Time.deltaTime * suavizadoInclinacion);
        }
        else
        {
            inclinacionActual = Mathf.Lerp(inclinacionActual, 0, Time.deltaTime * suavizadoInclinacion);
        }

        // Aplicar rotación al modelo visual (DEF-Body)
        if (modeloVisual != null)
        {
            modeloVisual.localEulerAngles = new Vector3(0f, inclinacionActual, 0f);
        }
    }
}