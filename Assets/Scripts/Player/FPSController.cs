using System.Collections.Specialized;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float jumpHeight = 2f;
    public float gravity = 9.81f;

    [Header("Look Settings")]
    public float mouseSensitivity = 0.1f;
    public Transform playerCamera;
    public float cameraClampAngle = 90f;
    public float lookSmoothTime = 0.05f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private float jumpCooldown = 0.2f; // 0.1f -> 100ms
    private float jumpTimer = 0f;

    private Vector2 currentMouseDelta;
    private Vector2 smoothedMouseDelta;
    private Vector2 mouseDeltaVelocity;

    private Vector2 moveInput;

    private float yaw = 0f;
    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Vector3 gravityDirection = GetGravityDirection();
        AlignPlayerWithGravity(gravityDirection);
        HandleLook();
        HandleMovement(gravityDirection);
        SnapToSurface(gravityDirection);
    }

    #region Gravity
    private Vector3 GetGravityDirection()
    {
        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("GravitySource");
        GameObject nearestObject = gameObjects[0];

        for (int i = 0; i < gameObjects.Length; i++)
        {
            if (Vector3.Distance(transform.position, gameObjects[i].transform.position) < Vector3.Distance(transform.position, nearestObject.transform.position))
            {
                nearestObject = gameObjects[i];
            }
        }

        Vector3 gravityDirection = (nearestObject.transform.position - transform.position).normalized;

        return gravityDirection;
    }

    private void AlignPlayerWithGravity(Vector3 gravityDirection)
    {
        // Align player up with -gravity
        Quaternion gravityRotation = Quaternion.FromToRotation(Vector3.up, -gravityDirection);

        // Apply yaw around gravity up axis
        transform.rotation = gravityRotation * Quaternion.Euler(0f, yaw, 0f);
    }

    private bool CheckGrounded(Vector3 gravityDirection)
    {
        // Start at the center of the character
        Vector3 origin = transform.position;

        // Length of the ray should be at least half the character height plus a small buffer
        float rayDistance = controller.height / 2 + 0.1f;

        UnityEngine.Debug.DrawRay(transform.position, gravityDirection * rayDistance, Color.red);

        return Physics.Raycast(origin, gravityDirection, out RaycastHit hit, rayDistance);
    }
    #endregion

    #region Look
    public void OnLook(InputAction.CallbackContext context)
    {
        currentMouseDelta = context.ReadValue<Vector2>();
    }

    private void HandleLook()
    {
        // Smooth the mouse input
        smoothedMouseDelta = Vector2.SmoothDamp(smoothedMouseDelta, currentMouseDelta, ref mouseDeltaVelocity, lookSmoothTime);

        // Apply smoothed rotation
        yaw += smoothedMouseDelta.x * mouseSensitivity;
        xRotation -= smoothedMouseDelta.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -cameraClampAngle, cameraClampAngle);
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Reset the raw input for the next frame
        currentMouseDelta = Vector2.zero;
    }
    #endregion

    #region Movement
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private bool CheckCanJump(Vector3 gravityDirection)
    {
        // Start at the center of the character
        Vector3 origin = transform.position;

        // Length of the ray should be at least half the character height plus a small buffer
        float rayDistance = controller.height / 2 + 0.3f;

        UnityEngine.Debug.DrawRay(transform.position, gravityDirection * rayDistance, Color.blue);

        return Physics.Raycast(origin, gravityDirection, out RaycastHit hit, rayDistance);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        Vector3 gravityDirection = GetGravityDirection();

        if (context.performed && CheckCanJump(gravityDirection))
        {
            //UnityEngine.Debug.Log("Jump");
            velocity += -gravityDirection * Mathf.Sqrt(jumpHeight * 2f * gravity);
            jumpTimer = jumpCooldown;
        }
    }

    private void HandleMovement(Vector3 gravityDirection)
    {
        isGrounded = CheckGrounded(gravityDirection);

        // Tangent directions
        Vector3 forward = Vector3.Cross(transform.right, -gravityDirection).normalized;
        Vector3 right = Vector3.Cross(-gravityDirection, forward).normalized;

        Vector3 horizontalMove = right * moveInput.x + forward * moveInput.y;
        horizontalMove *= walkSpeed;

        // Apply horizontal movement directly (instant)
        Vector3 horizontalVelocity = horizontalMove;

        // Apply gravity to vertical velocity
        Vector3 verticalVelocity = Vector3.Project(velocity, gravityDirection) + gravityDirection * gravity * Time.deltaTime;

        // Combine velocities
        velocity = horizontalVelocity + verticalVelocity;

        // Cancel downward velocity if grounded
        if (isGrounded && Vector3.Dot(velocity, gravityDirection) > 0f)
        {
            // Zero out velocity along gravity direction when grounded
            velocity -= Vector3.Project(velocity, gravityDirection);
        }

        // Move the player
        controller.Move(velocity * Time.deltaTime);

        // Tiny snap to ground
        if (isGrounded)
        {
            controller.Move(-gravityDirection * 0.01f);
        }
    }

    private void SnapToSurface(Vector3 gravityDir)
    {
        RaycastHit hit;
        float snapDistance = 0.2f;
        if (Physics.Raycast(transform.position, -gravityDir, out hit, snapDistance))
        {
            Vector3 snap = hit.point - transform.position;
            controller.Move(snap);
        }
    }
    #endregion
}
