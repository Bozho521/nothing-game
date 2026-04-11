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
    public float gravity = -19.62f; 
    public float jumpHeight = 2f;

    [Header("Camera & Look Settings")]
    public Transform cameraTransform; 
    public float mouseSensitivity = 0.2f;
    private float xRotation = 0f;

    [Header("New Input Actions")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction lookAction;
    public InputAction interactAction;

    [Header("UI Elements")]
    public GameObject deathScreenUI; 

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
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        lookAction.Disable();
        interactAction.Disable();
    }

    private void Update()
    {
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
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

        xRotation -= lookInput.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        if (jumpAction.triggered && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMovement = (move * speed) + (Vector3.up * velocity.y);

        controller.Move(finalMovement * Time.deltaTime);

        CheckInteraction();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

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
        }
        
        Time.timeScale = 0f; 
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
            Debug.Log("Interacting");
            interactable.Interact(this);
        }
    }

    public void OnInteractComplete(IInteractable interacted)
    {
        throw new System.NotImplementedException();
    }
}