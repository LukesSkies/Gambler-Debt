using System;
using System.Collections.Generic;
using UnityEngine;

// Records the events and measurements from one wave. It does not decide whether
// performance was good or how difficult the next wave should be.
public sealed class WavePerformanceRecorder : MonoBehaviour
{
    public static WavePerformanceRecorder Instance { get; private set; }

    // Runtime statistics are exposed through read-only properties, not Inspector fields.
    private int currentRound;
    private int playerCount = 1;
    private float damageDealt;
    private float damageTaken;
    private int pointsEarned;

    private float waveStartTime;
    private float waveEndTime;
    private float accumulatedMovementTime;
    // Sum of movement-sample delta times, including stationary samples. This is
    // used only to detect that movement sampling is connected, not as moving time.
    private float movementSampleTime;

    private bool hasStarted;
    private bool hasEnded;
    private bool hasFirstDamage;
    private float firstDamageTime;

    // Every player's last kill time is tracked independently.
    private sealed class PlayerKillHistory
    {
        public bool hasKill;
        public float lastKillElapsedTime;
        public float intervalSum;
        public int intervalCount;
    }

    private readonly Dictionary<int, PlayerKillHistory> playerKillHistory = new Dictionary<int, PlayerKillHistory>();
    private int killIntervalCount;

    // Configuration actually used for this wave. Tempo/baseline calculations
    // must compare results against these values, not only the round number.
    private int configuredDemonCount;
    private float configuredDemonHealth;
    private float configuredSpawnInterval;
    private int configuredSpawnBatchLimit;
    private int configuredMaximumActiveDemons;
    private float configuredDemonSwipeDamage;

    public event Action<WavePerformanceSnapshot> WaveCompleted;

    public int CurrentRound => currentRound;
    public int PlayerCount => playerCount;
    public float DamageDealt => damageDealt;
    public float DamageTaken => damageTaken;
    public int PointsEarned => pointsEarned;

    public bool HasStarted => hasStarted;
    public bool HasEnded => hasEnded;
    public bool HasMovementSamples => movementSampleTime > 0f;
    public bool HasFirstDamage => hasFirstDamage;
    public float FirstDamageTime => firstDamageTime;


    // Average moving seconds per configured player, keeping this timer comparable
    // with the round timer even when several players are being sampled.

    public float MovementTime => playerCount > 0 ? accumulatedMovementTime / playerCount : 0f;

    public float ActiveCombatTime => movementSampleTime;

    // True after at least one player has made two confirmed kills.
    public bool HasKillCadence => killIntervalCount > 0;


    // Average of each eligible player's own average kill interval. Every player
    // with at least one interval contributes equally, so a prolific player does
    // not completely dominate the team's Kill Cadence metric.

    public float AverageTimeBetweenKills
    {
        get
        {
            float sumOfPlayerAverages = 0f;
            int playersWithCadence = 0;

            foreach (PlayerKillHistory history in playerKillHistory.Values)
            {
                if (history.intervalCount <= 0)
                    continue;

                sumOfPlayerAverages += history.intervalSum / history.intervalCount;
                playersWithCadence++;
            }

            return playersWithCadence > 0 ? sumOfPlayerAverages / playersWithCadence : 0f;
        }
    }

    public float ElapsedTime
    {
        get
        {
            if (!hasStarted)
                return 0f;

            float endTime = hasEnded ? waveEndTime : Time.time;
            return Mathf.Max(0f, endTime - waveStartTime);
        }
    }


    // Percentage of the full round during which players were moving. Stationary
    // time stays in the denominator. Each player's movement is sampled separately,
    // so the result remains 0-100% regardless of whether there are 1 or 4 players.

    public float MovementPercentage => ElapsedTime > 0f
        ? Mathf.Clamp(accumulatedMovementTime / (ElapsedTime * Mathf.Max(1, playerCount)) * 100f, 0f, 100f)
        : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple WavePerformanceRecorder instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


    // Production entry point. Pass the exact configuration the WaveSpawner will
    // execute so TempoDirector and WaveBaselineManager know the actual workload.
    // Call before spawning the first demon.

    public void BeginWave(WaveConfiguration configuration)
    {
        if (configuration == null)
        {
            Debug.LogError("WavePerformanceRecorder.BeginWave received a null WaveConfiguration.");
            return;
        }

        BeginWave(
            configuration.roundNumber,
            configuration.playerCount,
            configuration.totalDemonCount,
            configuration.baseDemonHealth,
            configuration.spawnInterval,
            configuration.spawnBatchLimit,
            configuration.maximumActiveDemons,
            configuration.baseDemonSwipeDamage);
    }


    // Fallback/test overload for keyboard testers or old callers without a config.
    // Production wave flow should use BeginWave(WaveConfiguration).

    public void BeginWave(
        int roundNumber,
        int activePlayerCount = 1,
        int demonCount = 0,
        float demonHealth = 150f,
        float spawnInterval = 2f,
        int spawnBatchLimit = 1,
        int maximumActiveDemons = 0,
        float demonSwipeDamage = 0f)
    {
        if (hasStarted && !hasEnded)
        {
            Debug.LogWarning(
                $"Starting Round {roundNumber} while Round {currentRound} is still being recorded. " +
                "The previous recording will be discarded.");
        }

        currentRound = Mathf.Max(1, roundNumber);
        playerCount = Mathf.Clamp(activePlayerCount, 1, 4);
        configuredDemonCount = Mathf.Max(0, demonCount);
        // A zero count identifies a fallback/test call with no configuration.
        configuredDemonHealth = demonCount > 0 ? Mathf.Max(1f, demonHealth) : 0f;
        configuredSpawnInterval = Mathf.Max(0f, spawnInterval);
        configuredSpawnBatchLimit = Mathf.Max(1, spawnBatchLimit);
        configuredMaximumActiveDemons = Mathf.Max(0, maximumActiveDemons);
        configuredDemonSwipeDamage = Mathf.Max(0f, demonSwipeDamage);

        damageDealt = 0f;
        damageTaken = 0f;
        pointsEarned = 0;
        accumulatedMovementTime = 0f;
        movementSampleTime = 0f;
        hasFirstDamage = false;
        firstDamageTime = 0f;

        playerKillHistory.Clear();
        killIntervalCount = 0;

        waveStartTime = Time.time;
        waveEndTime = waveStartTime;
        hasStarted = true;
        hasEnded = false;

        Debug.Log($"Recorder: Round {currentRound} started for {playerCount} player(s).");
    }


    // Reports the health actually removed by a successful player attack. The
    // first successful hit sets Time Dmg. This parameter identifies the player
    // only for future player-specific analytics; the current UI displays team total.

    public void RecordDamageDealt(int playerId, float actualDamage)
    {
        if (!CanRecord() || actualDamage <= 0f)
            return;

        damageDealt += actualDamage;
        if (!hasFirstDamage)
        {
            hasFirstDamage = true;
            firstDamageTime = ElapsedTime;
        }
    }

    // Compatibility overload when the attacker id is not available.</summary>
    public void RecordDamageDealt(float actualDamage) => RecordDamageDealt(0, actualDamage);

    // Records damage actually received by the team after mitigation.</summary>
    public void RecordDamageTaken(float actualDamage)
    {
        if (!CanRecord() || actualDamage <= 0f)
            return;

        damageTaken += actualDamage;
    }


    // Records a confirmed kill for a specific player. Use the player's stable id,
    // not a temporary list index that may change during the wave.

    public void RecordKill(int playerId)
    {
        if (!CanRecord())
            return;

        float elapsedAtKill = ElapsedTime;
        if (!playerKillHistory.TryGetValue(playerId, out PlayerKillHistory history))
        {
            history = new PlayerKillHistory();
            playerKillHistory.Add(playerId, history);
        }

        if (history.hasKill)
        {
            float interval = Mathf.Max(0f, elapsedAtKill - history.lastKillElapsedTime);
            history.intervalSum += interval;
            history.intervalCount++;
            killIntervalCount++;
        }

        history.hasKill = true;
        history.lastKillElapsedTime = elapsedAtKill;
    }


    // Compatibility overload for solo tests/older code. For multiplayer, call
    // RecordKill(playerId), or kills from every player will be treated as one player.

    public void RecordKill() => RecordKill(0);

    // Record points only after the game actually awards them.</summary>
    public void RecordPointsEarned(int points)
    {
        if (!CanRecord() || points <= 0)
            return;

        pointsEarned += points;
    }


    // Record a player's actual movement state once per frame. Call this while
    // stationary too: isMoving=false advances the sample time but not MovementTime.

    public void RecordMovement(bool isMoving, float deltaTime)
    {
        if (!CanRecord() || deltaTime <= 0f)
            return;

        movementSampleTime += deltaTime;
        if (isMoving)
            accumulatedMovementTime += deltaTime;
    }


    // Freezes the recording and publishes a completed snapshot. Call only after
    // all scheduled demons have spawned and all demons assigned to this wave are dead.

    public void EndWave()
    {
        if (!CanRecord())
            return;

        waveEndTime = Time.time;
        hasEnded = true;
        WavePerformanceSnapshot snapshot = BuildSnapshot();

        Debug.Log(
            $"Round {currentRound} recorded: {snapshot.duration:F1}s, " +
            $"average per-player kill cadence: {FormatKillTime(snapshot)}, " +
            $"damage taken: {snapshot.damageTaken:F0}, points: {snapshot.pointsEarned}.");

        WaveCompleted?.Invoke(snapshot);
    }

    // Live snapshot for TempoDebugUI. Returns null before the first wave begins;
    // values freeze after EndWave. Getting a snapshot never changes statistics.
    public WavePerformanceSnapshot GetCurrentSnapshot()
    {
        if (!hasStarted)
            return null;

        return BuildSnapshot();
    }

    private WavePerformanceSnapshot BuildSnapshot()
    {
        return new WavePerformanceSnapshot
        {
            roundNumber = currentRound,
            playerCount = playerCount,
            duration = ElapsedTime,
            isComplete = hasEnded,
            damageDealt = damageDealt,
            damageTaken = damageTaken,
            pointsEarned = pointsEarned,
            hasFirstDamage = hasFirstDamage,
            firstDamageTime = firstDamageTime,
            hasKillCadence = HasKillCadence,
            averageTimeBetweenKills = AverageTimeBetweenKills,
            movementTime = MovementTime,
            activeCombatTime = movementSampleTime,
            movementPercentage = MovementPercentage,
            configuredDemonCount = configuredDemonCount,
            configuredDemonHealth = configuredDemonHealth,
            configuredSpawnInterval = configuredSpawnInterval,
            configuredSpawnBatchLimit = configuredSpawnBatchLimit,
            configuredMaximumActiveDemons = configuredMaximumActiveDemons,
            configuredDemonSwipeDamage = configuredDemonSwipeDamage
        };
    }

    private bool CanRecord() => hasStarted && !hasEnded;

    private static string FormatKillTime(WavePerformanceSnapshot snapshot)
    {
        return snapshot.hasKillCadence
            ? $"{snapshot.averageTimeBetweenKills:F2}s"
            : "not enough per-player kill intervals";
    }
}

// The measurements and the exact workload configuration for one wave. The
// Recorder creates this for the live UI and publishes it once when the wave ends.
[Serializable]
public sealed class WavePerformanceSnapshot
{
    public int roundNumber;
    public int playerCount;
    public float duration;
    public bool isComplete;

    public float damageDealt;
    public float damageTaken;
    public int pointsEarned;

    public bool hasFirstDamage;
    public float firstDamageTime;

    public bool hasKillCadence;
    public float averageTimeBetweenKills;

    public float movementTime;
    public float activeCombatTime;
    public float movementPercentage;

    public int configuredDemonCount;
    public float configuredDemonHealth;
    public float configuredSpawnInterval;
    public int configuredSpawnBatchLimit;
    public int configuredMaximumActiveDemons;
    public float configuredDemonSwipeDamage;
}
