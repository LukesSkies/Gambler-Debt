using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(10000)]
[DisallowMultipleComponent]
public sealed class WaveEnemyRecorderBridge : MonoBehaviour
{
    private static readonly FieldInfo EnemyHealthField = typeof(EnemyHealth).GetField(
        "_health",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo SwipeDamageField = typeof(ZombieAttackCollider).GetField(
        "_zombieDamage",
        BindingFlags.Instance | BindingFlags.NonPublic);

    // Existing damage calls do not supply attacker identity; current solo game uses player 0.
    private int killingPlayerId;

    private EnemyHealth enemyHealth;
    private WavePerformanceRecorder recorder;
    private float lastObservedHealth;
    private float configuredDemonHealth;
    private float configuredSwipeDamage = 50f;
    private bool hasConfiguration;
    private bool hasHealthBaseline;
    private bool killRecorded;
    private bool warnedAboutHealthField;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        recorder = WavePerformanceRecorder.Instance;
    }

    private void Start()
    {
        // This component runs late so the existing EnemyHealth.Start can initialise
        // its normal value first. We then apply the exact health from WaveConfiguration.
        ApplyConfiguredHealth();
        EstablishHealthBaseline();
        ApplySwipeDamage();
    }

    // Called by WaveSpawner immediately after the existing ZombieSpawnPoint
    // has created the zombie.
    public void Configure(float demonHealth, float swipeDamage, int creditedPlayerId = 0)
    {
        configuredDemonHealth = Mathf.Max(1f, demonHealth);
        configuredSwipeDamage = Mathf.Max(0f, swipeDamage);
        killingPlayerId = creditedPlayerId;
        hasConfiguration = true;

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (recorder == null)
            recorder = WavePerformanceRecorder.Instance;

        // The exact initial health is applied in Start, after EnemyHealth.Start
        // has assigned its legacy GameManager-based value.
        ApplySwipeDamage();
    }

    private void LateUpdate()
    {
        if (!hasHealthBaseline)
            EstablishHealthBaseline();

        ObserveHealth();
    }

    private void OnDestroy()
    {
        // A lethal hit can destroy the enemy before LateUpdate. Capture its final
        // health change here so the last hit is not lost from Damage Dealt.
        ObserveHealth();

        float currentHealth = ReadStoredHealth();
        if (hasHealthBaseline && currentHealth <= 0f && !killRecorded)
        {
            if (recorder == null)
                recorder = WavePerformanceRecorder.Instance;

            if (recorder != null)
                recorder.RecordKill(killingPlayerId);

            killRecorded = true;
        }
    }

    private void ApplyConfiguredHealth()
    {
        if (!hasConfiguration || enemyHealth == null || EnemyHealthField == null)
            return;

        EnemyHealthField.SetValue(enemyHealth, configuredDemonHealth);
        enemyHealth.Health = configuredDemonHealth;
    }

    private void EstablishHealthBaseline()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (enemyHealth == null)
            return;

        float health = ReadStoredHealth();
        if (health < 0f)
            return;

        lastObservedHealth = health;
        hasHealthBaseline = true;
    }

    private void ObserveHealth()
    {
        if (!hasHealthBaseline || enemyHealth == null)
            return;

        float currentHealth = ReadStoredHealth();
        if (currentHealth < 0f)
            return;

        float actualHealthRemoved = lastObservedHealth - currentHealth;
        if (actualHealthRemoved > 0f)
        {
            if (recorder == null)
                recorder = WavePerformanceRecorder.Instance;

            if (recorder != null)
                recorder.RecordDamageDealt(killingPlayerId, actualHealthRemoved);
        }

        lastObservedHealth = currentHealth;

        if (currentHealth <= 0f && !killRecorded)
        {
            if (recorder == null)
                recorder = WavePerformanceRecorder.Instance;

            if (recorder != null)
                recorder.RecordKill(killingPlayerId);

            killRecorded = true;
        }
    }

    private float ReadStoredHealth()
    {
        if (enemyHealth == null || EnemyHealthField == null)
        {
            if (!warnedAboutHealthField)
            {
                Debug.LogWarning(
                    "WaveEnemyRecorderBridge could not read EnemyHealth._health. " +
                    "Damage Dealt and kill recording will not work until the field is available.",
                    this);
                warnedAboutHealthField = true;
            }

            return -1f;
        }

        return (float)EnemyHealthField.GetValue(enemyHealth);
    }

    private void ApplySwipeDamage()
    {
        if (SwipeDamageField == null)
            return;

        ZombieAttackCollider[] attackColliders = GetComponentsInChildren<ZombieAttackCollider>(true);
        foreach (ZombieAttackCollider attackCollider in attackColliders)
        {
            SwipeDamageField.SetValue(attackCollider, configuredSwipeDamage);
        }
    }
}
