using Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour, IInteractor
{
    [Header("Player Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;

    [Header("Movement Stats")]
    public float speed = 6f;
    public float sprintSpeed = 10f; 
    public float gravity = -19.62f; 
    public float jumpHeight = 2f;

    [Header("Movement Constraints")]
    public bool canLookUpAndDown = false; 
    public bool canJump = false;
    public bool canSprint = false;

    [Header("Camera & Look Settings")]
    public Transform cameraTransform; 
    public float mouseSensitivity = 0.2f;
    private float xRotation = 0f;

    [Header("New Input Actions")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction lookAction;
    public InputAction interactAction;
    public InputAction sprintAction;      

    [Header("UI Elements")]
    public GameObject deathScreenUI; 

    [Header("Gun Animation Input")]
    public Animator gunAnimator;
    public string forwardsParam = "Forwards";
    public string backwardsParam = "Backwards";
    public string leftwardsParam = "Leftwards";
    public string rightwardsParam = "Rightwards";
    [Range(0f, 0.5f)] public float moveInputDeadzone = 0.1f;

    [Header("Measured Move Input (Read-Only)")]
    [Range(0f, 1f)] public float forwardsInput;
    [Range(0f, 1f)] public float backwardsInput;
    [Range(0f, 1f)] public float leftwardsInput;
    [Range(0f, 1f)] public float rightwardsInput;

    [Header("Player SFX")] 
    [SerializeField] private AK.Wwise.Event TakeDamageSound;

    public bool canInteract = true;
    
    private CharacterController controller;
    [SerializeField]
    private Camera playerCamera; 
    private Vector3 velocity;

    private void Start()
    {
        currentHealth = maxHealth;
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        
        Time.timeScale = 1f;
        
        if (deathScreenUI != null) deathScreenUI.SetActive(false);
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateHP(currentHealth); 

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        lookAction.Enable();
        interactAction.Enable();
        sprintAction.Enable(); 
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        lookAction.Disable();
        interactAction.Disable();
        sprintAction.Disable(); 
    }

    private void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.isPaused) return;

        if (isDead) 
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartGame();
            }
            return; 
        }

        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return; 

        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

        if (canLookUpAndDown)
        {
            xRotation -= lookInput.y * mouseSensitivity;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        MeasureDirectionalInput(moveInput);
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        if (jumpAction.triggered && controller.isGrounded && canJump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;

        float currentSpeed = speed;
        if (canSprint && sprintAction.IsPressed())
        {
            currentSpeed = sprintSpeed;
        }

        Vector3 finalMovement = (move * currentSpeed) + (Vector3.up * velocity.y);
        controller.Move(finalMovement * Time.deltaTime);

        CheckInteraction();
    }

    private void MeasureDirectionalInput(Vector2 moveInput)
    {
        Vector2 processedInput = ApplyDeadzone(moveInput, moveInputDeadzone);

        forwardsInput = Mathf.Max(0f, processedInput.y);
        backwardsInput = Mathf.Max(0f, -processedInput.y);
        rightwardsInput = Mathf.Max(0f, processedInput.x);
        leftwardsInput = Mathf.Max(0f, -processedInput.x);

        if (gunAnimator == null) return;

        gunAnimator.SetFloat(forwardsParam, forwardsInput);
        gunAnimator.SetFloat(backwardsParam, backwardsInput);
        gunAnimator.SetFloat(leftwardsParam, leftwardsInput);
        gunAnimator.SetFloat(rightwardsParam, rightwardsInput);
    }

    private static Vector2 ApplyDeadzone(Vector2 value, float deadzone)
    {
        float magnitude = value.magnitude;
        if (magnitude <= deadzone) return Vector2.zero;

        float scaledMagnitude = Mathf.InverseLerp(deadzone, 1f, Mathf.Clamp01(magnitude));
        return value.normalized * scaledMagnitude;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        TakeDamageSound.Post(gameObject);
        currentHealth -= damage;
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateHP(currentHealth);

        StartCoroutine(ScreenShake(0.15f, 0.2f)); 

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        
        if (deathScreenUI != null)
        {
            deathScreenUI.SetActive(true);
            
            foreach (Transform child in deathScreenUI.transform)
            {
                child.gameObject.SetActive(true);
            }
        }
        
        Time.timeScale = 0f; 
        
        Gun playerGun = FindFirstObjectByType<Gun>();
        if (playerGun != null) playerGun.UpdateCursorAndPlayerState();
    }

    private void RestartGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator ScreenShake(float duration, float magnitude)
    {
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cameraTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.unscaledDeltaTime; 

            yield return null;
        }

        cameraTransform.localPosition = originalPos;
    }

    public void CheckInteraction()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
        
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;
        if (!hit.collider.TryGetComponent<IInteractable>(out var interactable)) return;
        
        if (interactAction.triggered && canInteract)
        {
            interactable.Interact(this);
        }
    }

    public void OnInteractComplete(IInteractable interacted)
    {
        throw new System.NotImplementedException();
    }

    public void UnlockMovementConstraints()
    {
        canLookUpAndDown = true;
        canJump = true;
        canSprint = true; 
    }
}