using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics; // Importante para HapticImpulsePlayer
using UnityEngine.XR.Content.Interaction;

public class ControlMoto : MonoBehaviour
{
    [Header("Controles")]
    public XRKnob acelerador;
    public InputActionProperty frenoAction;
    public Transform camaraVR;
    public Transform modeloVisual; 
    
    [Header("Vibración (Haptics)")]
    // Cambiamos el tipo de variable para que coincida con lo que tienes en el Inspector
    public HapticImpulsePlayer hapticPlayer; 
    public float intensidadMaximaVibracion = 0.3f;

    [Header("Ajustes de Movimiento")]
    public float velocidadMaxima = 15f;
    public float fuerzaFrenado = 8f;
    
    [Header("Ajustes de Giro")]
    public float sensibilidadGiro = 40f;   
    public float maximaInclinacionVisual = 25f; 
    public float suavizadoInclinacion = 5f;

    private float velocidadActual = 0f;
    private float inclinacionActual = 0f;

    void Update()
    {
        float valorAcelerador = acelerador != null ? acelerador.value : 0f;
        float valorFreno = frenoAction.action.ReadValue<float>();

        // NUEVO SISTEMA DE VIBRACIÓN PARA UNITY 6
        if (valorAcelerador > 0.05f && hapticPlayer != null)
        {
            // Enviamos el impulso hípico
            hapticPlayer.SendHapticImpulse(valorAcelerador * intensidadMaximaVibracion, 0.1f);
        }

        ManejarMovimiento(valorAcelerador, valorFreno);
    }

    void ManejarMovimiento(float aceleracion, float freno)
    {
        if (freno > 0.1f)
            velocidadActual = Mathf.Lerp(velocidadActual, 0, Time.deltaTime * fuerzaFrenado * freno);
        else if (aceleracion > 0.05f)
            velocidadActual = Mathf.Lerp(velocidadActual, aceleracion * velocidadMaxima, Time.deltaTime);
        else
            velocidadActual = Mathf.Lerp(velocidadActual, 0, Time.deltaTime * 0.5f);

        if (velocidadActual > 0.1f)
        {
            // Usamos transform.up porque tu modelo está rotado -90 en X
            transform.Translate(-Vector3.up * velocidadActual * Time.deltaTime);
            
            float zRot = camaraVR.localEulerAngles.z;
            if (zRot > 180) zRot -= 360;
            float factorGiro = -Mathf.Clamp(zRot / 30f, -1f, 1f);
            transform.Rotate(0, 0, factorGiro * sensibilidadGiro * Time.deltaTime);
            
            float inclinacionDeseada = factorGiro * maximaInclinacionVisual;
            inclinacionActual = Mathf.Lerp(inclinacionActual, inclinacionDeseada, Time.deltaTime * suavizadoInclinacion);
        }
        else
        {
            inclinacionActual = Mathf.Lerp(inclinacionActual, 0, Time.deltaTime * suavizadoInclinacion);
        }

        if (modeloVisual != null)
            modeloVisual.localEulerAngles = new Vector3(0f, inclinacionActual, 0f);
    }
}