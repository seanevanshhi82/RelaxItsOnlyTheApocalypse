using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class TopDownPlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;

    [Header("Crouch Speeds")]
    public float crouchWalkSpeed = 3f;            // Slower walk while crouched
    public float crouchRunSpeed = 6f;             // Slower run while crouched

    [Header("Prone (Crawl) Speeds")]
    public float proneCrawlSpeed = 2f;            // Very slow crawl in prone

    [Header("Rotation")]
    public bool rotateToFaceMovement = true;
    public float rotationSpeed = 720f;

    [Header("Jump")]
    public float jumpHeight = 2.5f;
    public float gravity = -30f;

    [Header("Dodge")]
    public float dodgeDistance = 5f;
    public float dodgeDuration = 0.2f;
    public float dodgeCooldown = 5f;

    [Header("Crouch / Prone")]
    public float crouchHeightMultiplier = 0.6f;
    public float proneHeightMultiplier = 0.3f;
    public float crouchTransitionSpeed = 8f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer = 1;

    [Header("Animation")]
    public Animator animator;

    // ──────────────────────────────────────────────── Internal ────────────────
    private CharacterController controller;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private Vector3 velocity;
    private bool isGrounded;
    private float currentSpeed;

    // Dodge
    private bool isDodging;
    private float dodgeTimer;
    private float dodgeCooldownTimer;

    // Button state tracking
    private bool jumpWasPressedLastFrame;
    private bool crouchWasPressedLastFrame;
    private bool dodgeWasPressedLastFrame;

    // Posture
    private bool isCrouching;
    private bool isProne;

    // Original capsule
    private float originalHeight;
    private Vector3 originalCenter;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        if (controller == null || playerInput == null)
        {
            Debug.LogError("Missing CharacterController or PlayerInput!");
            enabled = false;
            return;
        }

        if (groundCheck == null)
        {
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0, -1f, 0);
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        originalHeight = controller.height;
        originalCenter = controller.center;
    }

    void Update()
    {
        // ─── Input Reading ─────────────────────────────────────────────
        moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        bool isRunning = playerInput.actions["Run"].IsPressed();
        bool crouchIsPressed = playerInput.actions["Crouch"].IsPressed();
        bool dodgeIsPressed = playerInput.actions["Dodge"].IsPressed();

        bool jumpIsPressed = playerInput.actions["Jump"].IsPressed();
        bool crouchPressedThisFrame = crouchIsPressed && !crouchWasPressedLastFrame;
        bool dodgePressedThisFrame = dodgeIsPressed && !dodgeWasPressedLastFrame;

        jumpWasPressedLastFrame = jumpIsPressed;
        crouchWasPressedLastFrame = crouchIsPressed;
        dodgeWasPressedLastFrame = dodgeIsPressed;

        // ─── Jump ──────────────────────────────────────────────────────
        if (jumpIsPressed && isGrounded && !isCrouching && !isProne)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // ─── Dodge ─────────────────────────────────────────────────────
        HandleDodge(dodgePressedThisFrame);

        // ─── Crouch / Prone + Posture ──────────────────────────────────
        HandleCrouchAndProne(crouchPressedThisFrame, crouchIsPressed);

        // ─── Speed Selection Based on Posture ──────────────────────────
        if (isProne)
        {
            currentSpeed = proneCrawlSpeed;  // Only crawl speed in prone
        }
        else if (isCrouching)
        {
            currentSpeed = isRunning ? crouchRunSpeed : crouchWalkSpeed;
        }
        else
        {
            currentSpeed = isRunning ? runSpeed : walkSpeed;
        }

        // ─── Movement ──────────────────────────────────────────────────
        Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        float moveSpeedThisFrame = isDodging ? (dodgeDistance / dodgeDuration) : currentSpeed;
        controller.Move(moveDir * moveSpeedThisFrame * Time.deltaTime);

        // ─── Rotation ──────────────────────────────────────────────────
        if (rotateToFaceMovement && moveDir.magnitude > 0.1f)
        {
            Quaternion target = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        // ─── Gravity & Ground Check ────────────────────────────────────
        isGrounded = groundCheck != null && Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -5f;
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // ─── Animation (Speed + Grounded) ──────────────────────────────
        if (animator != null)
        {
            float animSpeed = moveDir.magnitude * currentSpeed;
            if (isDodging) animSpeed = runSpeed;  // Force run blend during dodge

            animator.SetFloat("Speed", animSpeed);
            animator.SetBool("Grounded", isGrounded);
        }

        // ─── Capsule Resize (Sync with Posture) ────────────────────────
        float targetHeight = originalHeight;
        Vector3 targetCenter = originalCenter;

        if (isProne)
        {
            targetHeight *= proneHeightMultiplier;
            targetCenter.y = targetHeight * 0.5f;
        }
        else if (isCrouching)
        {
            targetHeight *= crouchHeightMultiplier;
            targetCenter.y = targetHeight * 0.5f;
        }

        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.center = Vector3.Lerp(controller.center, targetCenter, crouchTransitionSpeed * Time.deltaTime);
    }

    private void HandleDodge(bool dodgePressedThisFrame)
    {
        if (dodgeCooldownTimer > 0) dodgeCooldownTimer -= Time.deltaTime;

        if (isDodging)
        {
            dodgeTimer -= Time.deltaTime;
            if (dodgeTimer <= 0)
            {
                isDodging = false;
                dodgeCooldownTimer = dodgeCooldown;
            }
            return;
        }

        if (dodgePressedThisFrame && dodgeCooldownTimer <= 0)
        {
            Vector3 dodgeDir = moveInput.sqrMagnitude > 0.1f
                ? new Vector3(moveInput.x, 0f, moveInput.y).normalized
                : transform.forward;

            isDodging = true;
            dodgeTimer = dodgeDuration;
        }
    }

    private void HandleCrouchAndProne(bool crouchPressedThisFrame, bool crouchIsHeld)
    {
        float desiredPosture = 0f;  // standing

        if (crouchIsHeld)
        {
            isProne = true;
            isCrouching = false;
            desiredPosture = 10f;  // prone branch
        }
        else if (crouchPressedThisFrame)
        {
            isCrouching = !isCrouching;
            isProne = false;
            desiredPosture = isCrouching ? 5f : 0f;  // crouch or stand
        }
        else
        {
            isCrouching = false;
            isProne = false;
            desiredPosture = 0f;  // standing
        }

        // Apply Posture for your nested blend tree
        if (animator != null)
        {
            animator.SetFloat("Posture", desiredPosture);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}