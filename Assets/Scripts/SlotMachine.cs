using UnityEngine;
using TMPro;
using System.Collections;

public class SlotMachine : MonoBehaviour, IInteractable, IDamageable
{
    // GENERAL SETTINGS
    [Header("Settings")]
    public int coinCost = 5;          // Cost to spin the machine normally
    public float spinTime = 2f;       // How long the spin animation lasts

    // REWARD DATA
    [Header("Rewards")]
    public GunData[] possibleGuns;          // Guns that can be rewarded
    public GameObject gunPickupPrefab;      // Pickup prefab to spawn
    public Transform rewardSpawnPoint;      // Where rewards appear

    // PAID SPIN CHANCES
    [Header("Paid Spin Chances")]
    [Range(0f, 1f)] public float paidCoinChance = 0.5f;
    [Range(0f, 1f)] public float paidGunChance = 0.3f;
    [Range(0f, 1f)] public float paidHealChance = 0.15f;
    [Range(0f, 1f)] public float paidDamageChance = 0.05f;

    // PUNCH SPIN CHANCES
    [Header("Punch Spin Chances")]
    [Range(0f, 1f)] public float punchDamageChance = 0.55f;
    [Range(0f, 1f)] public float punchCoinChance = 0.25f;
    [Range(0f, 1f)] public float punchHealChance = 0.12f;
    [Range(0f, 1f)] public float punchGunChance = 0.08f;

    // VISUAL FEEDBACK
    [Header("Screen Visuals")]
    public Renderer screenRenderer;          // Renderer for the machine screen
    public Color idleColor = Color.gray;
    public Color spinColor = Color.yellow;
    public Color winColor = Color.green;
    public Color loseColor = Color.red;
    public Color healColor = Color.cyan;
    public Color refundColor = Color.magenta;
    public float colorTransitionSpeed = 8f;  // How fast colors lerp

    // UI
    [Header("UI Prompt")]
    public TMP_Text interactPrompt;          // On-screen interaction text

    // STATE FLAGS
    private bool isSpinning;     // Prevents multiple spins at once
    private bool slotDisabled;   // Disabled after gun win
    private bool playerNearby;   // Player is in interaction range

    // PLAYER REFERENCES
    private PlayerInventory inventory;
    private PlayerHealth health;

    // COROUTINES
    private Coroutine spinRoutine;
    private Coroutine colorRoutine;

    void Start()
    {
        // Grab player references (assumes single instances)
        inventory = FindFirstObjectByType<PlayerInventory>();
        health = FindFirstObjectByType<PlayerHealth>();

        // Set initial screen color
        if (screenRenderer)
            screenRenderer.material.color = idleColor;

        // Hide prompt until player is nearby
        if (interactPrompt)
            interactPrompt.gameObject.SetActive(false);
    }

    void Update()
    {
        // If player is not nearby or no UI exists, do nothing
        if (!playerNearby || interactPrompt == null)
            return;

        // Update UI prompt based on machine state
        if (slotDisabled)
        {
            interactPrompt.text = "CLAIM YOUR REWARD";
        }
        else if (isSpinning)
        {
            interactPrompt.text = "SPINNING...";
        }
        else if (inventory.coins < coinCost)
        {
            interactPrompt.text = $"SLOT MACHINE\nREQUIRES {coinCost} GOLD";
        }
        else
        {
            interactPrompt.text = $"SPIN SLOT ({coinCost} GOLD)";
        }
    }

    // Checks players loaction
    public void SetPlayerNearby(bool nearby)
    {
        playerNearby = nearby;
        interactPrompt.gameObject.SetActive(nearby);
    }

    //PAID SPIN
    public void Interact()
    {
        // Block interaction if busy or disabled
        if (isSpinning || slotDisabled)
            return;

        // Attempt to spend coins
        if (!inventory.SpendCoins(coinCost))
        {
            FlashColor(loseColor);
            return;
        }

        StartSpin(false);
    }

    // FREE SPIN
    public void TakeDamage(float amount)
    {
        if (isSpinning || slotDisabled)
            return;

        StartSpin(true);
    }

    // Starts the spin coroutine
    void StartSpin(bool isPunchSpin)
    {
        if (spinRoutine != null)
            StopCoroutine(spinRoutine);

        spinRoutine = StartCoroutine(SpinRoutine(isPunchSpin));
    }

    // Handles spin timing and result resolution
    IEnumerator SpinRoutine(bool isPunchSpin)
    {
        isSpinning = true;
        FlashColor(spinColor);

        // Simulated spin delay
        yield return new WaitForSeconds(spinTime);

        // Roll a random value to determine outcome
        float roll = Random.value;

        if (isPunchSpin)
            ResolvePunchRoll(roll);
        else
            ResolvePaidRoll(roll);

        // Short delay before resetting visuals
        yield return new WaitForSeconds(0.75f);

        isSpinning = false;
        spinRoutine = null;

        if (!slotDisabled)
            FlashColor(idleColor);
    }

    // Resolves outcomes for paid spins
    void ResolvePaidRoll(float roll)
    {
        if (roll < paidDamageChance)
        {
            health.TakeDamage(Random.Range(10f, 25f));
            interactPrompt.text = "THE MACHINE BITES BACK!";
            FlashColor(loseColor);
        }
        else if (roll < paidDamageChance + paidHealChance)
        {
            health.Heal(Random.Range(10f, 25f));
            interactPrompt.text = "HEALED!";
            FlashColor(healColor);
        }
        else if (roll < paidDamageChance + paidHealChance + paidGunChance)
        {
            DropGun();
            interactPrompt.text = "YOU WON A GUN!";
        }
        else
        {
            inventory.AddCoins(Random.Range(1, coinCost + 2));
            interactPrompt.text = "COINS!";
            FlashColor(refundColor);
        }
    }

    // Resolves outcomes for punch spins (riskier)
    void ResolvePunchRoll(float roll)
    {
        if (roll < punchDamageChance)
        {
            health.TakeDamage(Random.Range(10f, 30f));
            interactPrompt.text = "BAD IDEA.";
            FlashColor(loseColor);
        }
        else if (roll < punchDamageChance + punchHealChance)
        {
            health.Heal(Random.Range(5f, 15f));
            interactPrompt.text = "LUCKY HIT!";
            FlashColor(healColor);
        }
        else if (roll < punchDamageChance + punchHealChance + punchGunChance)
        {
            DropGun();
            interactPrompt.text = "IMPOSSIBLE LUCK!";
        }
        else
        {
            inventory.AddCoins(Random.Range(1, 5));
            interactPrompt.text = "SPARE CHANGE";
            FlashColor(refundColor);
        }
    }

    // Spawns a random gun and disables the slot
    void DropGun()
    {
        if (possibleGuns.Length == 0 || !gunPickupPrefab || !rewardSpawnPoint)
            return;

        GunData gun = possibleGuns[Random.Range(0, possibleGuns.Length)];

        GameObject obj = Instantiate(
            gunPickupPrefab,
            rewardSpawnPoint.position,
            Quaternion.identity
        );

        GunPickup pickup = obj.GetComponent<GunPickup>();
        pickup.SetGunData(gun);
        pickup.originSlot = this;

        slotDisabled = true;
        FlashColor(winColor);
    }

    // Called by the gun pickup after being collected
    public void ReactivateSlot()
    {
        slotDisabled = false;
        FlashColor(idleColor);
    }

    // Smoothly transitions screen color
    void FlashColor(Color target)
    {
        if (!screenRenderer) return;

        if (colorRoutine != null)
            StopCoroutine(colorRoutine);

        colorRoutine = StartCoroutine(ColorRoutine(target));
    }

    IEnumerator ColorRoutine(Color target)
    {
        Color start = screenRenderer.material.color;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * colorTransitionSpeed;
            screenRenderer.material.color = Color.Lerp(start, target, t);
            yield return null;
        }

        colorRoutine = null;
    }
}
