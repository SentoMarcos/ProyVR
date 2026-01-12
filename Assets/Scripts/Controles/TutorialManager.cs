using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
        TiltHead,
        Done
    }

    [Header("Tutorial Image")]
    public Image tutorialImage;

    public Sprite gripSprite;
    public Sprite brakeSprite;
    public Sprite acelerarSprite;
    public Sprite girarSprite;


    [Header("UI")]
    public TextMeshProUGUI tutorialText;

    [Header("Inputs")]
    public InputActionProperty leftGrip;
    public InputActionProperty leftTrigger;
    public InputActionProperty rightAcceleration;

    [Header("Cabeza")]
    [Tooltip("Transform de la cabeza/cámara para detectar ladeo")]
    public Transform headTransform;
    [Tooltip("Grados de ladeo necesarios (roll) para pasar el paso de giro")]
    [Range(5f, 45f)] public float headTiltThresholdDegrees = 12f;

    [Header("Detection")]
    [Tooltip("Referencia al transform de la mano izquierda (controlador)")]
    public Transform leftHand;
    [Tooltip("Referencia al transform de la mano derecha (controlador)")]
    public Transform rightHand;

    [Tooltip("Collider (trigger) que define la zona del manillar izquierdo")]
    public Collider leftHandlebarZone;
    [Tooltip("Collider (trigger) que define la zona del manillar derecho")]
    public Collider rightHandlebarZone;

    [Tooltip("Distancia máxima para considerar la mano dentro de la zona")] 
    [Range(0.01f, 0.5f)] public float handlebarRadius = 0.1f;

    [Header("Estado (solo lectura)")]
    public bool leftHandInZone;
    public bool rightHandInZone;

    [Header("Rotación Aceleración")]
    [Tooltip("Grados de giro de la mano derecha necesarios para pasar el paso de acelerar")]
    [Range(5f, 90f)] public float rotationThresholdDegrees = 25f;

    [Header("Cambio de escena")]
    [Tooltip("Nombre de la escena a cargar al terminar el tutorial")]
    public string nextSceneName;

    private Quaternion initialHeadRotation;
    private bool headBaselineCaptured;

    private Quaternion initialRightHandRotation;
    private bool rotationBaselineCaptured;

    private TutorialStep currentStep = TutorialStep.MoveHandsToHandlebar;

    void Start()
    {
        UpdateText();
    }

    void OnEnable()
    {
        leftGrip.action?.Enable();
        leftTrigger.action?.Enable();
        rightAcceleration.action?.Enable();
    }

    void OnDisable()
    {
        leftGrip.action?.Disable();
        leftTrigger.action?.Disable();
        rightAcceleration.action?.Disable();
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
                EnsureAccelerationBaseline();

                if (HasRotatedEnough())
                    NextStep();
                break;

            case TutorialStep.TiltHead:
                EnsureHeadBaseline();

                if (HasTiltedHeadEnough())
                    NextStep();
                break;
        }
    }

    void NextStep()
    {
        currentStep++;
        rotationBaselineCaptured = false;
        headBaselineCaptured = false;
        UpdateText();

        if (currentStep == TutorialStep.Done)
            LoadNextScene();
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
                tutorialImage.sprite = acelerarSprite;
                break;

            case TutorialStep.TiltHead:
                tutorialText.text = "Inclina la cabeza para girar";
                tutorialImage.sprite = girarSprite; 
                break;

            case TutorialStep.Done:
                tutorialText.text = "¡Ahora a jugar!";
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

    private void EnsureAccelerationBaseline()
    {
        if (rotationBaselineCaptured || currentStep != TutorialStep.Accelerate)
            return;

        if (TryGetRightHandRotation(out var rotation))
        {
            initialRightHandRotation = rotation;
            rotationBaselineCaptured = true;
        }
    }

    private bool HasRotatedEnough()
    {
        if (!TryGetRightHandRotation(out var currentRotation))
            return false;

        if (!rotationBaselineCaptured)
        {
            initialRightHandRotation = currentRotation;
            rotationBaselineCaptured = true;
            return false;
        }

        float angle = Quaternion.Angle(initialRightHandRotation, currentRotation);
        return angle >= rotationThresholdDegrees;
    }

    private void EnsureHeadBaseline()
    {
        if (headBaselineCaptured || currentStep != TutorialStep.TiltHead)
            return;

        if (TryGetHeadRotation(out var rotation))
        {
            initialHeadRotation = rotation;
            headBaselineCaptured = true;
        }
    }

    private bool HasTiltedHeadEnough()
    {
        if (!TryGetHeadRotation(out var currentRotation))
            return false;

        if (!headBaselineCaptured)
        {
            initialHeadRotation = currentRotation;
            headBaselineCaptured = true;
            return false;
        }

        float angle = Quaternion.Angle(initialHeadRotation, currentRotation);

        // Solo medir componente de roll (ladeo). Obtenemos delta y convertimos a euler.
        Quaternion delta = Quaternion.Inverse(initialHeadRotation) * currentRotation;
        Vector3 deltaEuler = delta.eulerAngles;
        float roll = Mathf.DeltaAngle(0f, deltaEuler.z);

        return Mathf.Abs(roll) >= headTiltThresholdDegrees && angle >= Mathf.Abs(headTiltThresholdDegrees * 0.5f);
    }

    private bool TryGetRightHandRotation(out Quaternion rotation)
    {
        rotation = Quaternion.identity;

        if (rightAcceleration.action != null)
        {
            if (!rightAcceleration.action.enabled)
                rightAcceleration.action.Enable();

            try
            {
                rotation = rightAcceleration.action.ReadValue<Quaternion>();
                return true;
            }
            catch (System.InvalidOperationException)
            {
                // El tipo de acción no es de rotación; se intentará con el transform
            }
        }

        if (rightHand != null)
        {
            rotation = rightHand.rotation;
            return true;
        }

        return false;
    }

    private bool TryGetHeadRotation(out Quaternion rotation)
    {
        rotation = Quaternion.identity;

        if (headTransform != null)
        {
            rotation = headTransform.rotation;
            return true;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            rotation = cam.transform.rotation;
            return true;
        }

        return false;
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
            return;

        if (Application.isPlaying)
            SceneManager.LoadScene(nextSceneName);
    }

}
