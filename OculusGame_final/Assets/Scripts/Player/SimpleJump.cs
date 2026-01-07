using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;

public class SimpleJump : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private InputActionProperty jumpButton;

    private CharacterController characterController;
    private bool isGrounded;
    private float verticalVelocity;
    private float gravity = -9.81f;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            Debug.LogWarning("SimpleJump: No CharacterController found!");
        }
    }

    private void Update()
    {
        // Check if grounded
        isGrounded = characterController.isGrounded;

        // Get jump button input
        bool jumpPressed = jumpButton.action?.WasPressedThisFrame() ?? false;

        // Jump logic
        if (jumpPressed && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        // Apply gravity
        if (!isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        else if (verticalVelocity < 0)
        {
            verticalVelocity = -2f; // Small downward force
        }

        // Apply vertical movement
        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}