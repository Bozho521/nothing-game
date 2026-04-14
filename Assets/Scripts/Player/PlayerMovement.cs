using Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerCamera), typeof(PlayerHealth), typeof(PlayerInteraction))]
public class PlayerMovement : MonoBehaviour, IInteractor
{
    [Header("Movement Stats")]
    public float speed = 6f;
    public float sprintSpeed = 10f; 
    public float gravity = -19.62f; 
    public float jumpHeight = 2f;

    [Header("Jump Game Feel")]
    public float fallMultiplier = 2.5f; 
    public float lowJumpMultiplier = 2f; 

    [Header("Bhop & Momentum")]
    public float groundAcceleration = 15f;
    public float airAcceleration = 5f;
    public float maxBhopSpeed = 16f; 

    [Header("Constraints")]
    public bool canJump = false;
    public bool canSprint = false;

    [Header("Input Actions")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction lookAction;
    public InputAction interactAction;
    public InputAction sprintAction;      

    [Header("Gun Animation Sync")]
    public Animator gunAnimator;
    public string forwardsParam = "Forwards";
    public string backwardsParam = "Backwards";
    public string leftwardsParam = "Leftwards";
    public string rightwardsParam = "Rightwards";
    [Range(0f, 0.5f)] public float moveInputDeadzone = 0.1f;

    [Header("Grace Zone Status")]
    public bool isInGraceZone { get; private set; }

    private CharacterController controller;
    private PlayerCamera playerCam;
    private PlayerHealth health;
    private PlayerInteraction interaction;
    
    private Vector3 velocity;
    private Vector3 horizontalMomentum; 

    public int currentHealth { get => health.currentHealth; set => health.SetHealth(value); }
    public int maxHealth => health.maxHealth;
    public bool isDead => health.isDead;
    public void TakeDamage(int dmg) => health.TakeDamage(dmg);
    public void OnInteractComplete(IInteractable interacted) { }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerCam = GetComponent<PlayerCamera>();
        health = GetComponent<PlayerHealth>();
        interaction = GetComponent<PlayerInteraction>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        moveAction.Enable(); jumpAction.Enable(); lookAction.Enable(); 
        interactAction.Enable(); sprintAction.Enable(); 
    }

    private void OnDisable()
    {
        moveAction.Disable(); jumpAction.Disable(); lookAction.Disable(); 
        interactAction.Disable(); sprintAction.Disable(); 
    }

    private void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.isPaused) return;

        if (isDead) 
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) health.RestartGame();
            return; 
        }

        playerCam.ProcessLook(lookAction.ReadValue<Vector2>(), transform);
        HandleMovement();

        if (interactAction.triggered) interaction.CheckAndInteract(this);
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        UpdateAnimator(moveInput);

        Vector3 wishDir = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
        float targetSpeed = (canSprint && sprintAction.IsPressed()) ? sprintSpeed : speed;

        if (controller.isGrounded)
        {
            if (velocity.y < 0) velocity.y = -2f;

            horizontalMomentum = Vector3.Lerp(horizontalMomentum, wishDir * targetSpeed, groundAcceleration * Time.deltaTime);

            if (jumpAction.triggered && canJump)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                
                horizontalMomentum += wishDir * 1.5f;
            }
        }
        else
        {
            horizontalMomentum += wishDir * airAcceleration * Time.deltaTime;

            if (horizontalMomentum.magnitude > maxBhopSpeed)
            {
                horizontalMomentum = horizontalMomentum.normalized * maxBhopSpeed;
            }

            if (velocity.y < 0)
            {
                velocity.y += gravity * fallMultiplier * Time.deltaTime;
            }
            else if (velocity.y > 0 && !jumpAction.IsPressed())
            {
                velocity.y += gravity * lowJumpMultiplier * Time.deltaTime;
            }
            else
            {
                velocity.y += gravity * Time.deltaTime;
            }
        }

        Vector3 finalMovement = horizontalMomentum + (Vector3.up * velocity.y);
        controller.Move(finalMovement * Time.deltaTime);
    }

    public void UnlockMovementConstraints()
    {
        playerCam.canLookUpAndDown = true;
        canJump = true;
        canSprint = true; 
    }

    private void UpdateAnimator(Vector2 moveInput)
    {
        if (gunAnimator == null) return;
        
        float magnitude = moveInput.magnitude;
        if (magnitude <= moveInputDeadzone) moveInput = Vector2.zero;
        else moveInput = moveInput.normalized * Mathf.InverseLerp(moveInputDeadzone, 1f, Mathf.Clamp01(magnitude));

        gunAnimator.SetFloat(forwardsParam, Mathf.Max(0f, moveInput.y));
        gunAnimator.SetFloat(backwardsParam, Mathf.Max(0f, -moveInput.y));
        gunAnimator.SetFloat(rightwardsParam, Mathf.Max(0f, moveInput.x));
        gunAnimator.SetFloat(leftwardsParam, Mathf.Max(0f, -moveInput.x));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("GraceZone"))
        {
            isInGraceZone = true;
            DespawnAllEnemies();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("GraceZone")) isInGraceZone = false;
    }

    private void DespawnAllEnemies()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && enemy.isActiveAndEnabled) Destroy(enemy.gameObject);
        }
    }
}