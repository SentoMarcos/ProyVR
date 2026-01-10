using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class ControlMoto : MonoBehaviour
{
    public XRKnob acelerador;
    public Transform camaraVR; // Arrastra la Main Camera aquí
    public float velocidadMaxima = 10f;
    
    [Header("Configuración de Giro")]
    public float sensibilidadGiro = 40f;   // Qué tanto gira la moto
    public float maximaInclinacion = 25f; // Qué tanto se inclina visualmente
    public float suavizado = 5f;          // Suavidad del movimiento

    void Update()
    {
        float potencia = acelerador.value;

        // 1. MOVIMIENTO (Tu código corregido)
        if (potencia > 0.05f)
        {
            transform.Translate(-Vector3.up * potencia * velocidadMaxima * Time.deltaTime);
            
            // 2. DETECCIÓN DE INCLINACIÓN DE CABEZA
            // Obtenemos la rotación Z de la cámara (rango -180 a 180)
            float inclinacionCabeza = camaraVR.localEulerAngles.z;
            if (inclinacionCabeza > 180) inclinacionCabeza -= 360;

            // Invertimos el valor para que sea intuitivo (inclinar derecha -> girar derecha)
            // Usamos un valor negativo porque suele venir invertido en el eje Z de la cámara
            float factorGiro = -inclinacionCabeza / 45f; // Normalizado
            factorGiro = Mathf.Clamp(factorGiro, -1f, 1f);

            // 3. GIRAR LA MOTO (Eje Y)
            // Solo giramos si estamos en movimiento
            float rotacionY = factorGiro * sensibilidadGiro * Time.deltaTime;
            transform.Rotate(0, 0, rotacionY); // Nota: Si tu moto está rotada -90 en X, 
                                               // quizás debas cambiar esto a (rotacionY, 0, 0)

            // 4. INCLINACIÓN VISUAL (Eje Z o X según tu modelo)
            // Esto es para que la moto se "tumbe" en las curvas
            // Creamos una rotación de inclinación basada en el factor de giro
            // Ajusta los ejes según la orientación de tu modelo (actualmente -90,0,0)
        }
    }
}