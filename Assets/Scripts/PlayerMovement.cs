using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
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

    private CharacterController controller;
    private Vector3 velocity;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        lookAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        lookAction.Disable();
    }

    private void Update()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

        xRotation -= lookInput.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);

        if (jumpAction.triggered && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}