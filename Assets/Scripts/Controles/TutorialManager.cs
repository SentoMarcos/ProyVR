using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public enum TutorialStep
    {
        MoveHandsToHandlebar,
        GrabHandlebar,
        Brake,
        Accelerate,
        Done
    }

    [Header("Tutorial Image")]
    public Image tutorialImage;

    public Sprite gripSprite;
    public Sprite brakeSprite;


    [Header("UI")]
    public TextMeshProUGUI tutorialText;

    [Header("Inputs")]
    public InputActionProperty leftGrip;
    public InputActionProperty leftTrigger;
    public InputActionProperty rightAcceleration;

    [Header("Detection")]
    [Tooltip("Referencia al transform de la mano izquierda (controlador)")]
    public Transform leftHand;
    [Tooltip("Referencia al transform de la mano derecha (controlador)")]
    public Transform rightHand;

    [Tooltip("Collider (trigger) que define la zona del manillar izquierdo")]
    public Collider leftHandlebarZone;
    [Tooltip("Collider (trigger) que define la zona del manillar derecho")]
    public Collider rightHandlebarZone;

    [Tooltip("Distancia mxima para considerar la mano dentro de la zona")] 
    [Range(0.01f, 0.5f)] public float handlebarRadius = 0.1f;

    [Header("Estado (solo lectura)")]
    public bool leftHandInZone;
    public bool rightHandInZone;

    private TutorialStep currentStep = TutorialStep.MoveHandsToHandlebar;

    void Start()
    {
        UpdateText();
    }

    void Update()
    {
        UpdateHandZones();

        switch (currentStep)
        {
            case TutorialStep.MoveHandsToHandlebar:
                if (leftHandInZone && rightHandInZone)
                    NextStep();
                break;

            case TutorialStep.GrabHandlebar:
                if (leftGrip.action.ReadValue<float>() > 0.8f)
                    NextStep();
                break;

            case TutorialStep.Brake:
                if (leftTrigger.action.ReadValue<float>() > 0.8f)
                    NextStep();
                break;

            case TutorialStep.Accelerate:
                if (rightAcceleration.action.ReadValue<float>() > 0.5f)
                    NextStep();
                break;
        }
    }

    void NextStep()
    {
        currentStep++;
        UpdateText();
    }
    void UpdateText()
    {
        tutorialImage.gameObject.SetActive(true);

        switch (currentStep)
        {
            case TutorialStep.MoveHandsToHandlebar:
                tutorialText.text = "Acerca las manos al manillar";
                tutorialImage.gameObject.SetActive(false);
                break;

            case TutorialStep.GrabHandlebar:
                tutorialText.text = "Pulsa el grip para coger el manillar";
                tutorialImage.gameObject.SetActive(true);
                tutorialImage.sprite = gripSprite;
                break;

            case TutorialStep.Brake:
                tutorialText.text = "Para frenar aprieta el trigger izquierdo";
                tutorialImage.sprite = brakeSprite;
                break;

            case TutorialStep.Accelerate:
                tutorialText.text = "Acelera girando la mano derecha";
                tutorialImage.gameObject.SetActive(false);
                break;

            case TutorialStep.Done:
                tutorialText.text = "�Ahora a jugar!";
                tutorialImage.gameObject.SetActive(false);
                break;
        }
    }

    private void UpdateHandZones()
    {
        leftHandInZone = IsHandInZone(leftHand, leftHandlebarZone);
        rightHandInZone = IsHandInZone(rightHand, rightHandlebarZone);
    }

    private bool IsHandInZone(Transform hand, Collider zone)
    {
        if (hand == null || zone == null) return false;

        Vector3 closest = zone.ClosestPoint(hand.position);
        float dist = Vector3.Distance(hand.position, closest);
        return dist <= handlebarRadius;
    }

}
