using UnityEngine;

public class FloatingElement : MonoBehaviour
{
    public float amplitude = 10f; // distancia máxima que flota
    public float speed = 2f;      // velocidad del movimiento

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position; // posición inicial
    }

    void Update()
    {
        // Movimiento sinusoidal
        float yOffset = Mathf.Sin(Time.time * speed) * amplitude;
        transform.position = startPos + new Vector3(0f, yOffset, 0f);
    }
}
