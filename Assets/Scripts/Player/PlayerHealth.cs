using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

// Handles player health, overshield, death state, and an interactive revive mechanic.
public class PlayerHealth : MonoBehaviour
{
    // HEALTH
    [Header("Health Settings")]
    public float maxHealth = 100f;            // Maximum base health
    public float currentHealth;               // Current base health

    // OVERSHIELD
    [Header("Overshield Settings")]
    public float maxOvershield = 50f;          // Maximum overshield value
    public float currentOvershield = 0f;       // Current overshield amount

    // REVIVE SYSTEM
    [Header("Revive Settings")]
    public int requiredPresses = 5;            // Button presses needed to revive
    public float reviveTimeLimit = 5f;         // Time window to succeed
    public float reviveHealthAmount = 50f;     // Health restored on revive
    public int pressIncreasePerDeath = 5;      // Difficulty scaling per death
    public Transform graveyardSpawnPoint;      // Teleport location on death

    // UI REFERENCES
    [Header("UI References")]
    public Slider healthSlider;                // Health bar
    public Slider overshieldSlider;             // Overshield bar
    public GameObject deathScreen;              // Death UI root
    public TMP_Text pressText;                  // Revive prompt text
    public TMP_Text timerText;                  // Revive countdown text

    // DEATH OPTIONS
    [Header("Death Options")]
    public Button retryButton;
    public Button quitButton;

    // STATE
    private PlayerControls controls;
    private bool isDead;                        // Player is currently dead
    private bool reviveStarted;                 // Revive coroutine active
    private int currentPresses;                 // Current revive presses
    private float originalTimeScale;            // Cached time scale
    private bool usingController;               // Input method detection

    void Awake()
    {
        // Initialize input and bind revive action
        controls = new PlayerControls();
        controls.OldPlayer.Revive.performed += _ => OnRevivePress();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
        UpdateUI();

        // Hide death UI by default
        if (deathScreen) deathScreen.SetActive(false);
        retryButton?.gameObject.SetActive(false);
        quitButton?.gameObject.SetActive(false);

        // Normalize overshield slider to 0–1 range
        if (overshieldSlider)
        {
            overshieldSlider.maxValue = 1;
            overshieldSlider.value = 0;
        }

        // Cache original time scale
        originalTimeScale = Time.timeScale;
    }

    void Update()
    {
        // Detect last-used input device for correct UI prompts
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
            usingController = true;
        else if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame)
            usingController = false;
    }

    // Applies damage, consuming overshield first.
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        float remaining = amount;

        // Absorb damage with overshield
        if (currentOvershield > 0)
        {
            float absorbed = Mathf.Min(currentOvershield, remaining);
            currentOvershield -= absorbed;
            remaining -= absorbed;
        }

        // Apply leftover damage to health
        if (remaining > 0)
        {
            currentHealth -= remaining;
            if (currentHealth <= 0)
                Die();
        }

        UpdateUI();
    }

    // Restores base health only.
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateUI();
    }

    // Adds overshield up to its maximum.
    public void AddOvershield(float amount)
    {
        currentOvershield = Mathf.Clamp(currentOvershield + amount, 0, maxOvershield);
        UpdateUI();
    }

    // Handles player death state and transition.
    void Die()
    {
        if (isDead) return;

        isDead = true;
        reviveStarted = false;
        currentPresses = 0;

        // Disable player combat and movement
        if (TryGetComponent<PlayerMovement>(out var move))
            move.enabled = false;
        if (TryGetComponent<PlayerMelee>(out var melee))
            melee.enabled = false;

        // Teleport to graveyard spawn
        if (graveyardSpawnPoint)
        {
            if (TryGetComponent<CharacterController>(out var cc))
            {
                cc.enabled = false;
                transform.SetPositionAndRotation(
                    graveyardSpawnPoint.position,
                    graveyardSpawnPoint.rotation
                );
                cc.enabled = true;
            }
            else
            {
                transform.SetPositionAndRotation(
                    graveyardSpawnPoint.position,
                    graveyardSpawnPoint.rotation
                );
            }
        }

        // Pause gameplay
        Time.timeScale = 0f;

        // Show death UI
        if (deathScreen) deathScreen.SetActive(true);

        retryButton?.gameObject.SetActive(false);
        quitButton?.gameObject.SetActive(false);

        // Initial revive prompt
        pressText.text = usingController
            ? "Press A to fight death"
            : "Press SPACE to fight death";

        timerText.text = "";

        // Keep cursor locked during revive phase
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Handles revive button presses.
    void OnRevivePress()
    {
        if (!isDead) return;

        // Start revive timer on first press
        if (!reviveStarted)
        {
            reviveStarted = true;
            StartCoroutine(ReviveRoutine());
            return;
        }

        // Count subsequent presses
        currentPresses++;
        pressText.text =
            $"PRESS {(usingController ? "A" : "SPACE")} ({currentPresses}/{requiredPresses})";
    }

    // Times the revive window and checks success.
    IEnumerator ReviveRoutine()
    {
        float timer = 0f;

        while (timer < reviveTimeLimit)
        {
            timer += Time.unscaledDeltaTime;
            timerText.text = $"TIME: {(reviveTimeLimit - timer):0.0}s";

            if (currentPresses >= requiredPresses)
            {
                RevivePlayer();
                yield break;
            }

            yield return null;
        }

        FailDeath();
    }

    // Called when revive fails.
    void FailDeath()
    {
        pressText.text = "DEBT PAID.";
        timerText.text = "YOU ARE DEAD.";

        retryButton?.gameObject.SetActive(true);
        quitButton?.gameObject.SetActive(true);

        retryButton.onClick.RemoveAllListeners();
        retryButton.onClick.AddListener(RetryGame);

        quitButton.onClick.RemoveAllListeners();
        quitButton.onClick.AddListener(QuitGame);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Restores player state after successful revive.
    void RevivePlayer()
    {
        isDead = false;

        currentHealth = Mathf.Min(maxHealth, reviveHealthAmount);
        UpdateUI();

        Time.timeScale = originalTimeScale;

        if (TryGetComponent<PlayerMovement>(out var move))
            move.enabled = true;
        if (TryGetComponent<PlayerMelee>(out var melee))
            melee.enabled = true;

        deathScreen.SetActive(false);

        // Increase revive difficulty for next death
        requiredPresses += pressIncreasePerDeath;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Reloads the current scene.
    public void RetryGame()
    {
        Time.timeScale = originalTimeScale;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    // Exits the game (and play mode in editor).
    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // UI

    //Updates health and overshield bars.
    void UpdateUI()
    {
        if (healthSlider)
            healthSlider.value = currentHealth / maxHealth;

        if (overshieldSlider)
            overshieldSlider.value = currentOvershield / maxOvershield;
    }
}
