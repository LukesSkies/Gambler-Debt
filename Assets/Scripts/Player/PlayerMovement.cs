using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // MOVEMENT SETTINGS
    public float walkSpeed = 10f;          // Target ground movement speed
    public float maxSpeed = 20f;           // Hard cap on horizontal velocity
    public float acceleration = 60f;       // How fast we accelerate on ground
    public float airAcceleration = 25f;    // How fast we accelerate in air
    public float deceleration = 20f;       // How fast we slow down when no input
    public float gravity = -40f;           // Downward force applied when airborne
    public float jumpForce = 12f;           // Initial upward velocity when jumping
    [Range(0f, 1f)] public float airControl = 0.7f; // Influence over movement while airborne

    // GROUND HANDLING
    public float slopeSnapDistance = 0.35f;    // Distance to snap to ground on slopes
    public LayerMask groundMask;               // What counts as ground
    public bool enableSlopeSnap = true;        // Enables slope snapping logic

    // BUNNYHOP / JUMP TIMING
    public bool enableAutoBunnyHop = false;    // Hold jump to auto-hop
    public float jumpCoyoteTime = 0.12f;       // Grace period after leaving ground
    public float jumpAcceptWindow = 0.15f;     // Input buffering window before landing

    // REFERENCES
    public Transform playerCamera;             // Used to move relative to view direction

    // INTERNAL STATE
    private CharacterController controller;    // Unity CharacterController
    private PlayerControls controls;           // Input system wrapper
    private Vector2 moveInput;                 // Raw movement input
    private bool jumpHeld;                     // Whether jump is being held
    private bool jumpPressedThisFrame;         // Jump pressed this frame only
    private float lastGroundedTime = -10f;     // Last time we were grounded
    private float lastJumpPressedTime = -10f;  // Last time jump was pressed

    private bool isGrounded;                   // Final grounded state
    private Vector3 velocity;                  // Current velocity vector
    private float groundCheckRadius = 0.2f;    // Reserved for ground probing

    void Awake()
    {
        // Cache required component
        controller = GetComponent<CharacterController>();

        // Initialize input actions
        controls = new PlayerControls();

        // Movement input
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // Jump input
        controls.Player.Jump.performed += ctx =>
        {
            jumpHeld = true;
            jumpPressedThisFrame = true;
            lastJumpPressedTime = Time.time;
        };
        controls.Player.Jump.canceled += ctx => jumpHeld = false;
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Update()
    {
        // CharacterController ground check
        bool controllerGrounded = controller.isGrounded;

        // Slope-based ground snapping
        bool slopeGrounded = false;
        if (enableSlopeSnap)
            slopeGrounded = CheckSlopeGround();

        // Final grounded state
        isGrounded = controllerGrounded || slopeGrounded;

        // Track last grounded time for coyote jump logic
        if (isGrounded)
            lastGroundedTime = Time.time;

        HandleMovement();
    }

    void HandleMovement()
    {
        // Camera-relative movement directions
        Vector3 camForward = playerCamera.forward;
        Vector3 camRight = playerCamera.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        // Build movement direction from input
        Vector3 moveDir = (camForward * moveInput.y + camRight * moveInput.x);
        float inputMag = moveDir.magnitude;
        if (inputMag > 1f) moveDir /= inputMag;

        // Extract horizontal velocity
        Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);

        if (isGrounded)
        {
            // Keep player grounded without sticking
            if (velocity.y < 0f)
                velocity.y = -2f;

            // Accelerate or decelerate on ground
            if (moveDir.sqrMagnitude > 0.001f)
            {
                Vector3 target = moveDir.normalized * walkSpeed;
                horizontalVel = Vector3.MoveTowards(horizontalVel, target, acceleration * Time.deltaTime);
            }
            else
            {
                horizontalVel = Vector3.MoveTowards(horizontalVel, Vector3.zero, deceleration * Time.deltaTime);
            }

            // Jump buffering and coyote time checks
            bool jumpRecentlyPressed =
                (Time.time - lastJumpPressedTime) <= jumpAcceptWindow || jumpPressedThisFrame;

            bool withinCoyote =
                (Time.time - lastGroundedTime) <= jumpCoyoteTime;

            // Normal jump
            if (jumpRecentlyPressed && withinCoyote)
            {
                velocity.y = jumpForce;

                jumpPressedThisFrame = false;
                lastJumpPressedTime = -10f;
            }
            // Auto bunny-hop
            else if (enableAutoBunnyHop && jumpHeld)
            {
                if (withinCoyote && (Time.time - lastGroundedTime) < 0.05f)
                    velocity.y = jumpForce;
            }
        }
        else
        {
            // Air movement
            Vector3 target = moveDir.normalized * walkSpeed;
            horizontalVel = Vector3.MoveTowards(
                horizontalVel,
                target,
                airAcceleration * airControl * Time.deltaTime
            );

            // Gravity
            velocity.y += gravity * Time.deltaTime;
        }

        // Apply horizontal velocity back
        velocity.x = horizontalVel.x;
        velocity.z = horizontalVel.z;

        // Clamp horizontal speed
        Vector3 flat = new Vector3(velocity.x, 0, velocity.z);
        if (flat.magnitude > maxSpeed)
        {
            Vector3 clamped = flat.normalized * maxSpeed;
            velocity.x = clamped.x;
            velocity.z = clamped.z;
        }

        // Move character
        controller.Move(velocity * Time.deltaTime);

        // Clear one-frame jump flag
        jumpPressedThisFrame = false;
    }

    bool CheckSlopeGround()
    {
        // Sphere cast slightly below player to detect slopes
        Vector3 worldCenter = transform.position + controller.center;
        Vector3 origin = worldCenter + Vector3.up * 0.1f;

        float sphereRadius = Mathf.Max(0.05f, controller.radius * 0.95f);
        float castDist = slopeSnapDistance + 0.1f;

        if (Physics.SphereCast(
            origin,
            sphereRadius,
            Vector3.down,
            out RaycastHit hit,
            castDist,
            groundMask,
            QueryTriggerInteraction.Ignore))
        {
            // Check if slope is walkable
            float groundAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (groundAngle <= controller.slopeLimit + 0.1f)
            {
                if (velocity.y < 0f)
                    velocity.y = -2f;

                return true;
            }
        }
        return false;
    }

    // PUBLIC ACCESSORS
    public Vector3 GetVelocity() => controller.velocity;
    public bool IsGrounded() => isGrounded;
}
