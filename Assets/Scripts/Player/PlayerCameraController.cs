using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class PlayerCameraController : MonoBehaviour
{
    // REFERENCES
    public Transform playerBody;
    public Transform weaponHolder;

    // LOOK SETTINGS (LEGACY - KEPT)
    public float mouseSensitivity = 1.5f;
    public float controllerSensitivity = 200f;
    public float verticalClamp = 85f;

    // LOOK SETTINGS
    [Header("Mouse Sensitivity")]
    public float mouseSensitivityX = 1.0f;
    public float mouseSensitivityY = 1.0f;
    public bool invertMouseX = false;
    public bool invertMouseY = false;

    [Header("Controller Sensitivity")]
    public float controllerSensitivityX = 200f;
    public float controllerSensitivityY = 200f;
    public bool invertControllerX = false;
    public bool invertControllerY = false;

    // CAMERA TILT
    public bool enableTilt = true;
    [Range(0f, 15f)] public float maxTilt = 10f;
    public float tiltSpeed = 8f;

    // HEADBOB
    public bool enableHeadbob = true;
    public float bobSpeed = 8f;
    public float bobAmount = 0.05f;

    // CLIPPING / CAMERA OFFSET
    public float baseNearClip = 0.05f;
    public float minNearClip = 0.02f;
    public float cameraOffset = 0.05f;

    public bool smoothWeaponFollow = true;
    public float weaponFollowSpeed = 15f;

    // INPUT / STATE
    private PlayerControls controls;
    private Vector2 lookInput;
    private Vector2 moveInput;

    private float xRotation;
    private float tiltTarget;
    private float currentTilt;
    private float bobTimer;

    // CACHED REFERENCES
    private Camera cam;
    private Vector3 startLocalPos;
    private Vector3 weaponHolderStartPos;

    void Awake()
    {
        controls = new PlayerControls();

        // Cache input values
        controls.OldPlayer.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.OldPlayer.Look.canceled += _ => lookInput = Vector2.zero;

        controls.OldPlayer.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.OldPlayer.Move.canceled += _ => moveInput = Vector2.zero;

        cam = GetComponent<Camera>();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Start()
    {
        // Lock and hide cursor for FPS-style camera
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        startLocalPos = transform.localPosition;

        // Push camera slightly forward to avoid clipping into player model
        transform.localPosition += Vector3.forward * cameraOffset;

        cam.nearClipPlane = Mathf.Max(minNearClip, baseNearClip);

        // Store initial weapon position for smoothing
        if (weaponHolder)
            weaponHolderStartPos = weaponHolder.localPosition;
    }

    void Update()
    {
        HandleLook();
        ApplyTilt();
        ApplyHeadbob();
        UpdateWeaponHolder();
    }

    void HandleLook()
    {
        bool usingMouse = Mouse.current != null && Mouse.current.delta.IsActuated();

        float lookX;
        float lookY;

        if (usingMouse)
        {
            float invertX = invertMouseX ? -1f : 1f;
            float invertY = invertMouseY ? -1f : 1f;

            lookX = lookInput.x * mouseSensitivityX * invertX;
            lookY = lookInput.y * mouseSensitivityY * invertY;
        }
        else
        {
            float invertX = invertControllerX ? -1f : 1f;
            float invertY = invertControllerY ? -1f : 1f;

            lookX = lookInput.x * controllerSensitivityX * invertX * Time.deltaTime;
            lookY = lookInput.y * controllerSensitivityY * invertY * Time.deltaTime;
        }

        // Vertical look (pitch)
        xRotation -= lookY;
        xRotation = Mathf.Clamp(xRotation, -verticalClamp, verticalClamp);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, currentTilt);

        // Horizontal look (yaw)
        playerBody.Rotate(Vector3.up * lookX);
    }

    void ApplyTilt()
    {
        if (!enableTilt) return;

        currentTilt = Mathf.Lerp(currentTilt, tiltTarget, Time.deltaTime * tiltSpeed);
        currentTilt = Mathf.Clamp(currentTilt, -maxTilt, maxTilt);
    }

    public void SetTilt(float inputX)
    {
        if (!enableTilt) return;

        tiltTarget = -inputX * maxTilt;
    }

    void ApplyHeadbob()
    {
        if (!enableHeadbob) return;

        bool isMoving = moveInput.magnitude > 0.1f;

        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobSpeed;

            transform.localPosition =
                startLocalPos +
                Vector3.forward * cameraOffset +
                Vector3.up * Mathf.Sin(bobTimer) * bobAmount;
        }
        else
        {
            bobTimer = 0f;

            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                startLocalPos + Vector3.forward * cameraOffset,
                Time.deltaTime * bobSpeed
            );
        }
    }

    void UpdateWeaponHolder()
    {
        if (!weaponHolder) return;

        if (smoothWeaponFollow)
        {
            weaponHolder.localPosition = Vector3.Lerp(
                weaponHolder.localPosition,
                weaponHolderStartPos,
                Time.deltaTime * weaponFollowSpeed
            );
        }
        
        // Enforce safe near clip every frame
        cam.nearClipPlane = Mathf.Max(minNearClip, baseNearClip);
    }

    public void ResetTilt() => tiltTarget = 0f;
}
