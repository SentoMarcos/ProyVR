using UnityEngine;
using UnityEngine.InputSystem;

public class TestingController : MonoBehaviour
{
    [Header("Input System")]
    public InputActionAsset inputActions;

    [Header("Componentes Físicos")]
    public CharacterController characterController;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundMask;

    [Header("Configuración Movimiento")]
    public float moveSpeed = 10f;
    public float rotationSpeed = 100f;
    public float speedIncrement = 5f;
    public float minSpeed = 5f;
    public float maxSpeed = 50f;
    public float crouchHeight = 1f;
    public float standingHeight = 2f;

    [Header("Referencias")]
    public ProceduralGenerator proceduralGenerator;
    public Camera playerCamera;

    // Variables privadas
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction speedUpAction;
    private InputAction speedDownAction;
    private InputAction resetAction;

    private float currentSpeed = 10f;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching = false;
    private bool isSprinting = false;
    private float originalStepOffset;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Obtener o añadir CharacterController
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
                Debug.Log("CharacterController añadido automáticamente");
            }
        }

        // Configurar CharacterController
        characterController.height = standingHeight;
        characterController.center = new Vector3(0, standingHeight / 2, 0);
        originalStepOffset = characterController.stepOffset;

        SetupInputSystem();
        Cursor.lockState = CursorLockMode.Locked;
        Debug.Log("TestingController FPS iniciado - WASD para mover, Espacio para saltar, Ctrl para agacharse");

        // Obtener o crear CharacterController AUTOMÁTICAMENTE
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
                Debug.Log("CharacterController añadido automáticamente");

                // Configurar valores por defecto
                characterController.height = 2.0f;
                characterController.radius = 0.5f;
                characterController.center = new Vector3(0, 1.0f, 0);
                characterController.slopeLimit = 45f;
                characterController.stepOffset = 0.3f;
                characterController.skinWidth = 0.08f;
                characterController.minMoveDistance = 0.0f;
            }
        }
    }

    void SetupInputSystem()
    {
        if (inputActions == null)
        {
            Debug.LogWarning("Input Actions Asset no asignado. Usando controles directos.");
            return;
        }

        try
        {
            inputActions.Enable();
            var playerMap = inputActions.FindActionMap("Player", true);

            // Obtener todas las acciones necesarias
            moveAction = playerMap.FindAction("Move", true);
            lookAction = playerMap.FindAction("Look", true);
            jumpAction = playerMap.FindAction("Jump", true);
            sprintAction = playerMap.FindAction("Sprint", true);
            crouchAction = playerMap.FindAction("Crouch", true);
            speedUpAction = playerMap.FindAction("SpeedUp", true);
            speedDownAction = playerMap.FindAction("SpeedDown", true);
            resetAction = playerMap.FindAction("Reset", true);

            // Configurar callbacks
            moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            moveAction.canceled += ctx => moveInput = Vector2.zero;

            lookAction.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
            lookAction.canceled += ctx => lookInput = Vector2.zero;

            jumpAction.performed += ctx => Jump();
            sprintAction.performed += ctx => isSprinting = true;
            sprintAction.canceled += ctx => isSprinting = false;
            crouchAction.performed += ctx => ToggleCrouch();
            speedUpAction.performed += ctx => AdjustSpeed(speedIncrement);
            speedDownAction.performed += ctx => AdjustSpeed(-speedIncrement);
            resetAction.performed += ctx => ResetPosition();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error configurando Input System: {e.Message}");
        }
    }

    void Update()
    {
        if (!enabled) return;

        HandleGroundCheck();
        HandleMovement();
        HandleRotation();
        HandleGravity();
        HandleCameraHeight();

        // Controles directos si no hay Input Actions
        if (inputActions == null)
        {
            HandleControlsDirect();
        }
    }

    void HandleGroundCheck()
    {
        // Raycast para verificar si está en el suelo
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;

        isGrounded = Physics.Raycast(rayStart, Vector3.down, out hit,
            characterController.height / 2 + groundCheckDistance, groundMask);

        // También usar el CharacterController
        isGrounded = isGrounded || characterController.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Pequeña fuerza hacia abajo para mantener contacto
            characterController.stepOffset = originalStepOffset;
        }
        else if (!isGrounded)
        {
            characterController.stepOffset = 0f;
        }
    }

    void HandleMovement()
    {
        if (moveInput.magnitude > 0 || (inputActions == null && IsMovementKeyPressed()))
        {
            // Calcular velocidad actual (incluye sprint)
            float actualSpeed = currentSpeed;
            if (isSprinting) actualSpeed *= 1.5f;
            if (isCrouching) actualSpeed *= 0.5f;

            // Obtener input
            Vector2 input = moveInput;
            if (inputActions == null)
            {
                input = GetDirectMovementInput();
            }

            // Calcular movimiento
            Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
            Vector3 movement = moveDirection * actualSpeed * Time.deltaTime;

            // Aplicar movimiento con CharacterController
            characterController.Move(movement);

            // Actualizar generador procedural
            UpdateProceduralGenerator(actualSpeed);
        }
    }

    Vector2 GetDirectMovementInput()
    {
        Vector2 input = Vector2.zero;
        var keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) input.y += 1;
            if (keyboard.sKey.isPressed) input.y -= 1;
            if (keyboard.aKey.isPressed) input.x -= 1;
            if (keyboard.dKey.isPressed) input.x += 1;

            // Sprint con Shift
            isSprinting = keyboard.leftShiftKey.isPressed;

            // Crouch con Ctrl
            if (keyboard.leftCtrlKey.wasPressedThisFrame)
            {
                ToggleCrouch();
            }
        }

        return input;
    }

    bool IsMovementKeyPressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return false;

        return keyboard.wKey.isPressed || keyboard.sKey.isPressed ||
               keyboard.aKey.isPressed || keyboard.dKey.isPressed;
    }

    void HandleRotation()
    {
        Vector2 rotationInput = lookInput;

        if (inputActions == null)
        {
            var mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                rotationInput = mouse.delta.ReadValue() * 0.1f; // Sensibilidad ajustada
            }
        }

        if (rotationInput.magnitude > 0 && Cursor.lockState == CursorLockMode.Locked)
        {
            // Rotación horizontal (Yaw)
            float yaw = rotationInput.x * rotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, yaw);

            // Rotación vertical (Pitch) - solo en la cámara
            float pitch = -rotationInput.y * rotationSpeed * Time.deltaTime;
            float currentPitch = playerCamera.transform.localEulerAngles.x;
            currentPitch = currentPitch > 180 ? currentPitch - 360 : currentPitch;
            float newPitch = Mathf.Clamp(currentPitch + pitch, -90f, 90f);

            playerCamera.transform.localEulerAngles = new Vector3(newPitch, 0, 0);
        }
    }

    void HandleGravity()
    {
        // Aplicar gravedad
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // Aplicar velocidad vertical
        characterController.Move(velocity * Time.deltaTime);
    }

    void HandleCameraHeight()
    {
        // Ajustar altura de la cámara según crouch
        float targetHeight = isCrouching ? crouchHeight : standingHeight;

        // Suavizar transición
        characterController.height = Mathf.Lerp(characterController.height, targetHeight, Time.deltaTime * 10f);
        characterController.center = new Vector3(0, characterController.height / 2, 0);

        // Ajustar posición de la cámara
        if (playerCamera != null)
        {
            float cameraHeight = isCrouching ? crouchHeight - 0.2f : standingHeight - 0.2f;
            Vector3 targetPos = new Vector3(0, cameraHeight, 0);
            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition, targetPos, Time.deltaTime * 10f);
        }
    }

    void HandleControlsDirect()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Salto con Espacio
        if (keyboard.spaceKey.wasPressedThisFrame && isGrounded)
        {
            Jump();
        }

        // Ajuste de velocidad con teclas personalizadas
        if (keyboard.rightShiftKey.wasPressedThisFrame)
        {
            AdjustSpeed(speedIncrement);
        }

        if (keyboard.rightCtrlKey.wasPressedThisFrame)
        {
            AdjustSpeed(-speedIncrement);
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            ResetPosition();
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ?
                CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    void Jump()
    {
        if (isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            Debug.Log("Salto ejecutado");
        }
    }

    void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        Debug.Log($"Agachado: {isCrouching}");
    }

    void UpdateProceduralGenerator(float speed)
    {
        if (proceduralGenerator != null)
        {
            proceduralGenerator.SetSpawnDistance(Mathf.Clamp(speed * 3f, 50f, 300f));
            proceduralGenerator.SetDestroyDistance(Mathf.Clamp(speed * 2f, 30f, 200f));
        }
    }

    void AdjustSpeed(float amount)
    {
        currentSpeed = Mathf.Clamp(currentSpeed + amount, minSpeed, maxSpeed);
        Debug.Log($"Velocidad actual: {currentSpeed}");
    }

    void ResetPosition()
    {
        // Teleport a posición segura
        transform.position = new Vector3(0, 2f, 0);
        velocity = Vector3.zero;
        Debug.Log("Posición reseteada");
    }

    void OnEnable()
    {
        inputActions?.Enable();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void OnDisable()
    {
        inputActions?.Disable();
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    // Para debugging
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Dibujar ray de ground check
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * (characterController.height / 2 + groundCheckDistance));
    }
}