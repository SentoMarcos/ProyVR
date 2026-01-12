using UnityEngine;

public class TeleportOnTriggerDual : MonoBehaviour
{
    public enum HandType { Left, Right }
    public HandType hand;          // Indica si es la mano izquierda o derecha
    public Transform visualHand;   // Objeto visual de la mano

    private bool teleported = false;

    // Coordenadas fijas
    private Vector3 leftPosition = new Vector3(-0.27389f, 0.84704f, 5.83388f);
    private Quaternion leftRotation = new Quaternion(0.103292f, 0.0663928f, 0.031674f, -0.991927f);

    private Vector3 rightPosition = new Vector3(0.26801f, 0.85994f, 5.81588f);
    private Quaternion rightRotation = new Quaternion(-0.105655f, 0.0625623f, 0.0680147f, 0.9901f);

    private void OnTriggerEnter(Collider other)
    {
        if (teleported) return; // Evita m�ltiples ejecuciones

        Debug.Log($"[{gameObject.name}] OnTriggerEnter con: {other.name}");

        if ((hand == HandType.Left && other.CompareTag("LeftFreezeZone")) ||
            (hand == HandType.Right && other.CompareTag("RightFreezeZone")))
        {
            Debug.Log($"[{gameObject.name}] Ha colisionado con su zona correcta");

            if (visualHand != null)
            {
                if (hand == HandType.Left)
                {
                    visualHand.position = leftPosition;
                    visualHand.rotation = leftRotation;
                }
                else // Right
                {
                    visualHand.position = rightPosition;
                    visualHand.rotation = rightRotation;
                }

                Debug.Log($"[{gameObject.name}] Se ha teletransportado la mano {hand}");
            }

            teleported = true;
        }
    }
}
