using UnityEngine;

[DisallowMultipleComponent]
public class LiquidWobble : MonoBehaviour
{
    [Header("Shader")]
    [SerializeField] private Renderer targetRenderer;

    private static readonly int RotXID = Shader.PropertyToID("_rotx");
    private static readonly int RotZID = Shader.PropertyToID("_rotz");

    private MaterialPropertyBlock propertyBlock;

    [Header("Wobble Settings")]
    [Tooltip("How quickly the liquid oscillates.")]
    [SerializeField, Min(0.1f)]
    private float frequency = 1.5f;

    [Tooltip("Approximate time for the wobble to decay back to zero.")]
    [SerializeField, Min(0.1f)]
    private float returnToZeroTime = 2f;

    [Tooltip("Maximum shader value the wobble can reach.")]
    [SerializeField, Min(0.001f)]
    private float maxWobble = 0.5f;

    [Header("Movement Sensitivity")]
    [Tooltip("How strongly movement affects the liquid.")]
    [SerializeField]
    private float movementSensitivity = 8f;

    [Header("Rotation Sensitivity")]
    [Tooltip("How strongly rotation affects the liquid.")]
    [SerializeField]
    private float rotationSensitivity = 0.5f;

    [Header("Axis Direction")]
    [SerializeField] private float xDirection = 1f;
    [SerializeField] private float zDirection = 1f;

    // Current wobble values.
    private float wobbleX;
    private float wobbleZ;

    // Wobble velocities.
    private float velocityX;
    private float velocityZ;

    // Previous transform data.
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    private Vector3 lastWorldVelocity;
    private Vector3 lastLocalAngularVelocity;

    private bool initialized;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        initialized = false;

        wobbleX = 0f;
        wobbleZ = 0f;

        velocityX = 0f;
        velocityZ = 0f;

        ApplyShaderValues();
    }

    private void LateUpdate()
    {
        if (targetRenderer == null)
            return;

        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;

        // Initialise our previous transform measurements.
        if (!initialized)
        {
            lastPosition = currentPosition;
            lastRotation = currentRotation;

            lastWorldVelocity = Vector3.zero;
            lastLocalAngularVelocity = Vector3.zero;

            initialized = true;
            return;
        }

        // MOVEMENT DETECTION
        Vector3 worldVelocity =
            (currentPosition - lastPosition) / dt;

        Vector3 worldAcceleration =
            (worldVelocity - lastWorldVelocity) / dt;

        // Convert acceleration into the object's local space.
        Vector3 localAcceleration =
            transform.InverseTransformDirection(worldAcceleration);

        // ROTATION DETECTION
        Quaternion rotationDelta =
            Quaternion.Inverse(lastRotation) * currentRotation;

        rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);

        // Convert the angle into a signed angle.
        if (angle > 180f)
            angle -= 360f;

        // Calculate local angular velocity in radians.
        Vector3 localAngularVelocity =
            axis * (angle * Mathf.Deg2Rad / dt);

        // Calculate angular acceleration.
        Vector3 localAngularAcceleration =
            (localAngularVelocity - lastLocalAngularVelocity) / dt;

        // CALCULATE LIQUID FORCES
        // Forward/backward movement affects X rotation.
        float forceX =
            -localAcceleration.z * movementSensitivity
            - localAngularAcceleration.x * rotationSensitivity;

        // Left/right movement affects Z rotation.
        float forceZ =
            localAcceleration.x * movementSensitivity
            - localAngularAcceleration.z * rotationSensitivity;

        forceX *= xDirection;
        forceZ *= zDirection;

        // UPDATE THE WOBBLE
        UpdateOscillator(ref wobbleX, ref velocityX, forceX, dt);
        UpdateOscillator(ref wobbleZ, ref velocityZ, forceZ, dt);

        // Store transform measurements for next frame.
        lastPosition = currentPosition;
        lastRotation = currentRotation;

        lastWorldVelocity = worldVelocity;
        lastLocalAngularVelocity = localAngularVelocity;

        // Update shader.
        ApplyShaderValues();
    }

    private void UpdateOscillator(
        ref float value,
        ref float velocity,
        float force,
        float dt)
    {
        // Natural frequency of the oscillator.
        float angularFrequency = 2f * Mathf.PI * frequency;

        // Spring strength.
        float springForce =
            angularFrequency * angularFrequency;

        // Damping rate.
        // At returnToZeroTime, approximately 95% of the
        // original oscillation amplitude has decayed.
        float dampingRate = 3f / returnToZeroTime;

        // Apply external movement/rotation force.
        velocity += force * dt;

        // Apply spring force pulling the liquid towards zero.
        velocity -= springForce * value * dt;

        // Apply smooth exponential damping.
        velocity *= Mathf.Exp(-dampingRate * dt);

        // Update wobble position.
        value += velocity * dt;

        // Prevent extreme values.
        value = Mathf.Clamp(value, -maxWobble, maxWobble);

        // Remove tiny leftover values to guarantee a clean rest.
        if (Mathf.Abs(value) < 0.0001f &&
            Mathf.Abs(velocity) < 0.0001f)
        {
            value = 0f;
            velocity = 0f;
        }
    }

    private void ApplyShaderValues()
    {
        if (targetRenderer == null)
            return;

        targetRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat(RotXID, wobbleX);
        propertyBlock.SetFloat(RotZID, wobbleZ);

        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDisable()
    {
        wobbleX = 0f;
        wobbleZ = 0f;

        velocityX = 0f;
        velocityZ = 0f;

        ApplyShaderValues();
    }
}
