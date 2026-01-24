using UnityEngine;

public class GunPickup : MonoBehaviour, IInteractable
{
    // DATA
    private GunData gunData;               // Gun this pickup will give the player

    // VISUAL SETTINGS
    public float spinSpeed = 90f;           // Degrees per second rotation
    public float floatAmplitude = 0.25f;    // Vertical bob height
    public float floatSpeed = 2f;           // Bobbing speed

    private Vector3 startPos;               // Initial position for bob offset

    // SLOT MACHINE LINK
    [HideInInspector]
    public SlotMachine originSlot;          // Slot machine that spawned this pickup

    void Start()
    {
        // Cache starting position for floating animation
        startPos = transform.position;
    }

    // Assigns the gun data after instantiation
    public void SetGunData(GunData data) => gunData = data;

    // Placeholder for future spin logic (kept for extensibility)
    public void StartSpinning() { }

    void Update()
    {
        // Continuous rotation for visibility
        transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime);

        // Floating bob animation using sine wave
        transform.position =
            startPos +
            Vector3.up * Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
    }

    // INTERACTION
    public void Interact()
    {
        // Safety check
        if (gunData == null) return;

        // Find player's gun system
        var playerGunSystem = FindFirstObjectByType<PlayerGunSystem>();
        if (playerGunSystem)
        {
            // Give gun to player
            playerGunSystem.GiveGun(gunData);

            // Debug feedback
            Debug.Log($"Picked up {gunData.gunName}!");

            // Re-enable the slot machine that spawned this gun
            originSlot?.ReactivateSlot();

            // Remove pickup from world
            Destroy(gameObject);
        }
    }
}
