using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Handles player melee attacks:
// Alternating left/right punches
// Sphere-based hit detection
// Arm swing visuals
// UI hit marker feedback
public class PlayerMelee : MonoBehaviour
{
    // MELEE SETTINGS
    [Header("Melee Settings")]
    public float meleeRange = 2f;              // Distance in front of the camera where the punch lands
    public float meleeRadius = 0.7f;           // Radius of the punch hit sphere
    public float meleeDamage = 25f;            // Damage dealt per punch
    public float meleeCooldown = 0.35f;        // Time between punches
    public LayerMask hitMask = ~0;              // What layers can be hit

    // ARM SWING VISUALS
    [Header("Arm Swing Visuals")]
    public Transform leftArm;                   // Left arm transform
    public Transform rightArm;                  // Right arm transform
    public Vector3 punchOutOffset = new Vector3(0, 0, 0.4f); // Forward punch movement
    public float punchSpeed = 10f;              // Speed of punch animation

    // UI HIT MARKER
    [Header("UI Hit-Marker")]
    public Image hitMarker;                     // Center-screen hit indicator
    public Color idleColor = Color.white;       // Default color
    public Color inRangeColor = Color.red;      // Color when something is punchable
    public Color hitFlashColor = new Color(1, 0, 0, 1); // Flash color on hit
    public float hitFlashTime = 0.1f;           // Flash duration

    // CAMERA
    [Header("Camera")]
    public Camera playerCamera;                 // Camera used for aiming punches

    // INTERNAL STATE
    private PlayerControls controls;            // Input actions
    private bool canMelee = true;               // Cooldown gate
    private bool isPunching;                    // Arm animation state
    private bool useLeftArm = true;             // Alternates punches
    private Vector3 leftRestPos;                // Left arm rest position
    private Vector3 rightRestPos;               // Right arm rest position
    private Coroutine flashRoutine;             // Hit flash coroutine reference

    void Awake()
    {
        // Initialize input system and bind melee action
        controls = new PlayerControls();
        controls.Player.Melee.performed += _ => TryMelee();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Start()
    {
        // Auto-assign camera if missing
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Cache arm rest positions for punch animation
        if (leftArm != null)
            leftRestPos = leftArm.localPosition;

        if (rightArm != null)
            rightRestPos = rightArm.localPosition;
    }

    void Update()
    {
        HandleArmSwing();          // (Reserved for future blending / sway logic)
        UpdateHitMarkerRange();    // Update UI based on punch reach
    }

    // Attempts to perform a melee attack.
    // Handles cooldown, hit detection, damage, visuals, and UI feedback.
    void TryMelee()
    {
        if (!canMelee || playerCamera == null)
            return;

        canMelee = false;

        // Select which arm punches this time
        Transform activeArm = useLeftArm ? leftArm : rightArm;

        // Start arm punch animation
        if (activeArm != null)
            StartCoroutine(PunchSwing(activeArm, useLeftArm));

        bool hitSomething = false;

        // Calculate punch origin and center
        Vector3 origin = playerCamera.transform.position;
        Vector3 forward = playerCamera.transform.forward;
        Vector3 center = origin + forward * meleeRange;

        // Detect hit colliders in a sphere
        Collider[] hits = Physics.OverlapSphere(
            center,
            meleeRadius,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (var col in hits)
        {
            // Ensure target is roughly in front of the player
            Vector3 toTarget = col.bounds.center - origin;
            if (Vector3.Dot(toTarget.normalized, forward) < 0.5f)
                continue;

            // Damage anything that implements IDamageable
            IDamageable dmg = col.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(meleeDamage);
                hitSomething = true;
            }
        }

        // Flash hit marker if something was hit
        if (hitSomething && hitMarker != null)
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(HitFlash());
        }

        // Alternate arm for next punch
        useLeftArm = !useLeftArm;

        // Reset cooldown after delay
        Invoke(nameof(ResetMelee), meleeCooldown);
    }

    // Animates the punch by pushing the arm forward and pulling it back.
    IEnumerator PunchSwing(Transform arm, bool isLeft)
    {
        isPunching = true;

        Vector3 start = isLeft ? leftRestPos : rightRestPos;
        Vector3 target = start + punchOutOffset;

        float t = 0f;

        // Punch forward
        while (t < 1f)
        {
            t += Time.deltaTime * punchSpeed;
            arm.localPosition = Vector3.Lerp(start, target, t);
            yield return null;
        }

        t = 0f;

        // Return to rest position
        while (t < 1f)
        {
            t += Time.deltaTime * punchSpeed;
            arm.localPosition = Vector3.Lerp(target, start, t);
            yield return null;
        }

        isPunching = false;
    }

    // Reserved hook for future arm sway or animation blending.
    void HandleArmSwing()
    {
    }

    // Resets melee cooldown.
    void ResetMelee() => canMelee = true;

    // Changes hit marker color when an enemy is in punch range.
    void UpdateHitMarkerRange()
    {
        if (hitMarker == null || playerCamera == null)
            return;

        bool inRange = Physics.CheckSphere(
            playerCamera.transform.position + playerCamera.transform.forward * meleeRange,
            meleeRadius,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        hitMarker.color = inRange ? inRangeColor : idleColor;
    }

    // Briefly flashes the hit marker when a punch connects.
    IEnumerator HitFlash()
    {
        hitMarker.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashTime);
        hitMarker.color = idleColor;
    }

    // Draws melee hit radius in the editor for tuning.
    void OnDrawGizmosSelected()
    {
        if (playerCamera == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            playerCamera.transform.position + playerCamera.transform.forward * meleeRange,
            meleeRadius
        );
    }
}
