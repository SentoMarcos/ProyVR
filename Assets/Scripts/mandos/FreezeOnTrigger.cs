using UnityEngine;

public class FreezeOnTrigger : MonoBehaviour
{
    public string freezeZoneTag;   // "LeftFreezeZone" o "RightFreezeZone"
    
    public TutorialManager tutorialManager;
    public bool isLeftHand;

    private bool frozen = false;
    private Vector3 frozenPosition;
    private Quaternion frozenRotation;

    void Update()
    {
        if (frozen)
        {
            transform.position = frozenPosition;
            transform.rotation = frozenRotation;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[{gameObject.name}] OnTriggerEnter con: {other.name}");

        if (other.CompareTag(freezeZoneTag))
        {
            Debug.Log($"[{gameObject.name}] Ha colisionado con su zona correcta: {freezeZoneTag}");

            frozen = true;

            frozenPosition = transform.position;
            frozenRotation = transform.rotation;

            Debug.Log($"[{gameObject.name}] Se ha congelado el mando");

            if (tutorialManager != null)
            {
                if (isLeftHand)
                    tutorialManager.leftHandInZone = true;
                else
                    tutorialManager.rightHandInZone = true;
            }

        }
        else
        {
            Debug.Log($"[{gameObject.name}] Ha colisionado con algo NO asignado");
        }
    }
}
