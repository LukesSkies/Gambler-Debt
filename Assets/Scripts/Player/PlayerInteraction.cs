using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Handles player interaction detection using a camera raycast.
// Updates crosshair color based on what the player is aiming at
// and triggers interactions when input is pressed.
public class PlayerInteraction : MonoBehaviour
{
    // RANGES
    [Header("Ranges")]
    public float interactRange = 3f;           // Distance for interactable objects
    public float enemyDetectionRange = 60f;    // Longer range just for enemy detection

    // REFERENCES
    [Header("References")]
    public Image crosshair;                    // UI crosshair image
    public LayerMask interactableMask = ~0;    // Layers that can be interacted with

    // CROSSHAIR COLORS
    [Header("Crosshair Colors")]
    public Color defaultColor = Color.white;   // Nothing targeted
    public Color enemyColor = Color.red;       // Enemy in sight
    public Color slotMachineColor = Color.green; // Slot machine targeted
    public Color interactColor = Color.cyan;   // Generic interactable targeted

    // STATE
    private Camera cam;                        // Player camera reference
    private PlayerControls controls;           // Input actions
    private IInteractable currentInteractable; // Cached interactable
    private SlotMachine currentSlot;           // Special-case slot machine reference

    void Awake()
    {
        // Initialize input and bind interact action
        controls = new PlayerControls();
        controls.Player.Interact.performed += _ => TryInteract();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Start()
    {
        // Cache camera and initialize crosshair
        cam = Camera.main;

        if (crosshair)
            crosshair.color = defaultColor;
    }

    void Update()
    {
        HandleRaycast();
    }

    // Performs raycasts from the camera to detect interactables,
    // slot machines, and enemies, updating crosshair color accordingly.
    void HandleRaycast()
    {
        if (!cam || !crosshair)
            return;

        // Reset crosshair and nearby state each frame
        crosshair.color = defaultColor;

        if (currentSlot != null)
            currentSlot.SetPlayerNearby(false);

        currentInteractable = null;
        currentSlot = null;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        // Short-range interactable check
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableMask))
        {
            currentInteractable = hit.collider.GetComponent<IInteractable>();

            // Slot machines get special UI handling
            if (hit.collider.TryGetComponent(out SlotMachine slot))
            {
                currentSlot = slot;
                slot.SetPlayerNearby(true);
                crosshair.color = slotMachineColor;
                return;
            }

            // Generic interactable
            if (currentInteractable != null)
            {
                crosshair.color = interactColor;
                return;
            }
        }

        // Long-range enemy detection (visual feedback only)
        if (Physics.Raycast(ray, out hit, enemyDetectionRange))
        {
            if (hit.collider.CompareTag("Enemy"))
                crosshair.color = enemyColor;
        }
    }

    // Attempts to interact with the currently targeted object.
    void TryInteract()
    {
        currentInteractable?.Interact();
    }
}
