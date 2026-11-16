using UnityEngine;

// One 0-100 score per measured performance category.
public struct WaveMetricScores
{
    public float roundTime;
    public float killCadence;
    public float timeToFirstDamage;
    public float movement;
    public float movementPercentage;
    public float damageTaken;
    public float damageDealt;
    public float points;

    public bool hasRoundTime;
    public bool hasKillCadence;
    public bool hasFirstDamage;
    public bool hasMovementSamples;
    public bool hasDamageTaken;
    public bool hasDamageDealt;
    public bool hasPoints;
}

// Calculates metric scores and learns per-run averages. All authored settings
// are read from WaveDirector, so this component has no tuning Inspector fields.
[DisallowMultipleComponent]
public sealed class WaveBaselineManager : MonoBehaviour
{
    public static WaveBaselineManager Instance { get; private set; }

    private WaveDirector waveDirector;

    // Runtime learning state only. These are deliberately not serialized into the Inspector.
    private float averageDurationRatio = 1f;
    private float averageKillCadenceRatio = 1f;
    private float averageFirstDamageRatio = 1f;
    private float averageDamageTakenRatio = 1f;
    private float averageMovementPercentage = 60f;
    private int completedWavesLearned;
    private bool initialized;

    public int CompletedWavesLearned => completedWavesLearned;
    public float AverageMovementPercentage => averageMovementPercentage;
    public float AverageDurationRatio => averageDurationRatio;
    public float AverageKillCadenceRatio => averageKillCadenceRatio;
    public float AverageFirstDamageRatio => averageFirstDamageRatio;
    public float AverageDamageTakenRatio => averageDamageTakenRatio;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple WaveBaselineManager instances detected. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveWaveDirector();
        ResetToDefaults();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ResetToDefaults()
    {
        ResolveWaveDirector();
        averageDurationRatio = 1f;
        averageKillCadenceRatio = 1f;
        averageFirstDamageRatio = 1f;
        averageDamageTakenRatio = 1f;
        averageMovementPercentage = waveDirector != null ? waveDirector.ExpectedMovementPercentage : 60f;
        completedWavesLearned = 0;
        initialized = true;
    }

    public WaveMetricScores GetMetricScores(WavePerformanceSnapshot snapshot)
    {
        EnsureInitialized();
        WaveMetricScores scores = NeutralScores();
        if (snapshot == null || waveDirector == null)
            return scores;

        float expectedDuration = EstimateExpectedRoundDuration(snapshot);
        float expectedKillCadence = EstimateExpectedKillCadence(snapshot);
        float expectedDamageTaken = EstimateExpectedDamageTaken(snapshot);
        float expectedTotalEnemyHealth = EstimateExpectedTotalEnemyHealth(snapshot);
        float expectedPointsTarget = EstimateExpectedPointsTarget(snapshot);
        float expectedFirstHit = EstimateExpectedFirstDamageTime(snapshot);

        scores.hasRoundTime = snapshot.duration > 0f && expectedDuration > 0f;
        if (scores.hasRoundTime)
        {
            float normalizedDuration = SafeRatio(snapshot.duration, expectedDuration);
            scores.roundTime = ScoreLowerIsBetter(normalizedDuration, averageDurationRatio);
        }

        scores.hasKillCadence = snapshot.hasKillCadence && expectedKillCadence > 0f;
        if (scores.hasKillCadence)
        {
            float normalizedCadence = SafeRatio(snapshot.averageTimeBetweenKills, expectedKillCadence);
            scores.killCadence = ScoreLowerIsBetter(normalizedCadence, averageKillCadenceRatio);
        }

        float observedFirstHitTime = snapshot.hasFirstDamage ? snapshot.firstDamageTime : snapshot.duration;
        scores.hasFirstDamage = snapshot.hasFirstDamage;
        if (expectedFirstHit > 0f)
        {
            float normalizedFirstHit = SafeRatio(observedFirstHitTime, expectedFirstHit);
            scores.timeToFirstDamage = ScoreLowerIsBetter(normalizedFirstHit, averageFirstDamageRatio);
        }

        scores.hasMovementSamples = snapshot.activeCombatTime > 0f;
        if (scores.hasMovementSamples)
        {
            float value = Mathf.Clamp(snapshot.movementPercentage, 0f, 100f);
            scores.movement = value;
            scores.movementPercentage = value;
        }

        scores.hasDamageTaken = snapshot.duration >= 0f;
        if (scores.hasDamageTaken)
        {
            float learnedExpectedDamage = expectedDamageTaken * averageDamageTakenRatio;
            scores.damageTaken = ScoreDamageTaken(snapshot.damageTaken, learnedExpectedDamage);
        }

        scores.hasDamageDealt = expectedTotalEnemyHealth > 0f;
        if (scores.hasDamageDealt)
        {
            float completionRatio = SafeRatio(snapshot.damageDealt, expectedTotalEnemyHealth);
            scores.damageDealt = Mathf.Clamp01(completionRatio) * 100f;
        }

        scores.hasPoints = expectedPointsTarget > 0f;
        if (scores.hasPoints)
        {
            // The calibrated average (550 by default in solo Round 1) maps to 75/100.
            float pointsRatio = SafeRatio(snapshot.pointsEarned, expectedPointsTarget);
            scores.points = Mathf.Clamp(pointsRatio * waveDirector.PointsScoreAtExpectedAverage, 0f, 100f);
        }

        return scores;
    }

    public void LearnFromCompletedWave(WavePerformanceSnapshot snapshot)
    {
        EnsureInitialized();
        if (snapshot == null || waveDirector == null)
            return;

        float expectedDuration = EstimateExpectedRoundDuration(snapshot);
        float expectedKillCadence = EstimateExpectedKillCadence(snapshot);
        float expectedDamageTaken = EstimateExpectedDamageTaken(snapshot);
        float expectedFirstHit = EstimateExpectedFirstDamageTime(snapshot);

        if (snapshot.duration > 0f && expectedDuration > 0f)
            LearnRatio(ref averageDurationRatio, snapshot.duration / expectedDuration);

        if (snapshot.hasKillCadence && expectedKillCadence > 0f)
            LearnRatio(ref averageKillCadenceRatio, snapshot.averageTimeBetweenKills / expectedKillCadence);

        if (snapshot.hasFirstDamage && expectedFirstHit > 0f)
            LearnRatio(ref averageFirstDamageRatio, snapshot.firstDamageTime / expectedFirstHit);

        if (snapshot.isComplete && snapshot.damageTaken > 0f && expectedDamageTaken > 0f)
            LearnRatio(ref averageDamageTakenRatio, snapshot.damageTaken / expectedDamageTaken);

        if (snapshot.activeCombatTime > 0f)
        {
            averageMovementPercentage = Mathf.Lerp(
                averageMovementPercentage,
                Mathf.Clamp(snapshot.movementPercentage, 0f, 100f),
                waveDirector.LearningRate);
        }

        completedWavesLearned++;
    }

    public float EstimateExpectedRoundDuration(WavePerformanceSnapshot snapshot)
    {
        EnsureWaveDirector();
        if (waveDirector == null)
            return 40f;
        if (snapshot == null)
            return waveDirector.RoundOneExpectedDuration;

        int players = Mathf.Clamp(snapshot.playerCount, 1, 4);
        int demons = GetExpectedDemonCount(snapshot);
        float health = GetExpectedDemonHealth(snapshot);

        float healthRatio = Mathf.Max(0.01f, health / waveDirector.RoundOneDemonHealth);
        float healthScale = Mathf.Pow(healthRatio, waveDirector.HealthWorkloadExponent);
        float playerScale = Mathf.Pow(players, waveDirector.PlayerWorkloadExponent);

        float workloadDuration = waveDirector.RoundOneExpectedDuration
            * (demons / (float)waveDirector.RoundOneDemonCount)
            * healthScale
            / Mathf.Max(1f, playerScale);

        int batchSize = Mathf.Max(1, snapshot.configuredSpawnBatchLimit);
        int batchCount = Mathf.CeilToInt(demons / (float)batchSize);
        float spawnInterval = snapshot.configuredSpawnInterval > 0f
            ? snapshot.configuredSpawnInterval
            : waveDirector.RoundOneSpawnInterval;
        float spawnScheduleDuration = Mathf.Max(0, batchCount - 1) * spawnInterval;

        return Mathf.Max(1f, Mathf.Max(workloadDuration, spawnScheduleDuration));
    }

    public float EstimateExpectedKillCadence(WavePerformanceSnapshot snapshot)
    {
        EnsureWaveDirector();
        if (waveDirector == null)
            return 5f;
        if (snapshot == null)
            return waveDirector.RoundOneExpectedKillCadence;

        float health = GetExpectedDemonHealth(snapshot);
        float healthRatio = Mathf.Max(0.01f, health / waveDirector.RoundOneDemonHealth);
        float healthScale = Mathf.Pow(healthRatio, waveDirector.HealthWorkloadExponent);

        int players = Mathf.Clamp(snapshot.playerCount, 1, 4);
        int activeCap = GetExpectedActiveCap(snapshot);
        float activeCapPerPlayer = activeCap / (float)players;
        float availabilityScale = Mathf.Sqrt(waveDirector.BaseActiveDemonCap / Mathf.Max(0.5f, activeCapPerPlayer));
        availabilityScale = Mathf.Clamp(availabilityScale, 0.5f, 2f);

        return Mathf.Max(0.1f, waveDirector.RoundOneExpectedKillCadence * healthScale * availabilityScale);
    }

    private float EstimateExpectedFirstDamageTime(WavePerformanceSnapshot snapshot)
    {
        EnsureWaveDirector();
        if (waveDirector == null)
            return 5f;
        int players = snapshot != null ? Mathf.Clamp(snapshot.playerCount, 1, 4) : 1;
        return Mathf.Max(0.1f, waveDirector.ExpectedFirstDamageTime / Mathf.Sqrt(players));
    }

    private float EstimateExpectedDamageTaken(WavePerformanceSnapshot snapshot)
    {
        EnsureWaveDirector();
        if (waveDirector == null || snapshot == null)
            return 0f;

        int players = Mathf.Clamp(snapshot.playerCount, 1, 4);
        int demons = GetExpectedDemonCount(snapshot);
        float demonHealth = GetExpectedDemonHealth(snapshot);
        float populationHealthRatio = (demons / (float)waveDirector.RoundOneDemonCount)
            * Mathf.Max(0.01f, demonHealth / waveDirector.RoundOneDemonHealth);
        float workloadScale = Mathf.Pow(Mathf.Max(0.1f, populationHealthRatio), waveDirector.DamageExposureExponent);

        float swipeDamage = snapshot.configuredDemonSwipeDamage > 0f
            ? snapshot.configuredDemonSwipeDamage
            : waveDirector.DemonSwipeDamage;
        float swipeScale = waveDirector.DemonSwipeDamage > 0f
            ? Mathf.Pow(Mathf.Max(0.01f, swipeDamage / waveDirector.DemonSwipeDamage), waveDirector.SwipeDamageExponent)
            : 1f;

        int activeCap = GetExpectedActiveCap(snapshot);
        float activeCapScale = Mathf.Sqrt(activeCap / (float)waveDirector.BaseActiveDemonCap);
        return waveDirector.RoundOneExpectedDamageTaken * players * workloadScale * swipeScale * activeCapScale;
    }

    private float EstimateExpectedTotalEnemyHealth(WavePerformanceSnapshot snapshot)
    {
        return GetExpectedDemonCount(snapshot) * GetExpectedDemonHealth(snapshot);
    }

    private float EstimateExpectedPointsTarget(WavePerformanceSnapshot snapshot)
    {
        // Points are calibrated against average real play, not theoretical maximum points.
        // Enemy health is intentionally excluded from this target.
        return waveDirector.RoundOneExpectedAveragePoints
            * (GetExpectedDemonCount(snapshot) / (float)waveDirector.RoundOneDemonCount);
    }

    private int GetExpectedDemonCount(WavePerformanceSnapshot snapshot)
    {
        if (snapshot.configuredDemonCount > 0)
            return snapshot.configuredDemonCount;

        int round = Mathf.Max(1, snapshot.roundNumber);
        int players = Mathf.Clamp(snapshot.playerCount, 1, 4);
        return Mathf.Max(1, waveDirector.RoundOneDemonCount
            + (round - 1) * waveDirector.AdditionalDemonsPerRound
            + (players - 1) * waveDirector.AdditionalDemonsPerExtraPlayer);
    }

    private float GetExpectedDemonHealth(WavePerformanceSnapshot snapshot)
    {
        if (snapshot.configuredDemonHealth > 0f)
            return snapshot.configuredDemonHealth;

        int round = Mathf.Max(1, snapshot.roundNumber);
        if (round <= 9)
            return waveDirector.RoundOneDemonHealth + (round - 1) * waveDirector.HealthIncreasePerRound;

        float roundNineHealth = waveDirector.RoundOneDemonHealth + 8f * waveDirector.HealthIncreasePerRound;
        return roundNineHealth * Mathf.Pow(1f + waveDirector.HealthIncreasePercentageFromRound10, round - 9);
    }

    private int GetExpectedActiveCap(WavePerformanceSnapshot snapshot)
    {
        if (snapshot.configuredMaximumActiveDemons > 0)
            return snapshot.configuredMaximumActiveDemons;

        int players = Mathf.Clamp(snapshot.playerCount, 1, 4);
        return Mathf.Clamp(waveDirector.BaseActiveDemonCap
            + (players - 1) * waveDirector.ExtraActiveCapPerPlayer,
            1, waveDirector.AbsoluteMaximumActiveDemons);
    }

    private void LearnRatio(ref float average, float observedRatio)
    {
        float minimum = 1f / waveDirector.MaximumObservedRatio;
        float clamped = Mathf.Clamp(observedRatio, minimum, waveDirector.MaximumObservedRatio);
        average = Mathf.Max(0.01f, Mathf.Lerp(average, clamped, waveDirector.LearningRate));
    }

    private static float SafeRatio(float actual, float expected)
    {
        return expected > 0f ? Mathf.Max(0f, actual) / expected : 1f;
    }

    private static float ScoreDamageTaken(float actualDamage, float expectedDamage)
    {
        actualDamage = Mathf.Max(0f, actualDamage);
        if (actualDamage <= 0f) return 100f;
        if (expectedDamage <= 0f) return 0f;

        float ratio = actualDamage / expectedDamage;
        if (ratio < 1f)
            return Mathf.Lerp(100f, 50f, Mathf.Clamp01(ratio));
        return Mathf.Lerp(49f, 0f, Mathf.Clamp01(ratio - 1f));
    }

    private static float ScoreLowerIsBetter(float actualRatio, float averageRatio)
    {
        if (averageRatio <= 0f) return 50f;
        return Mathf.Clamp(100f - (Mathf.Max(0f, actualRatio) / averageRatio) * 50f, 0f, 100f);
    }

    private static WaveMetricScores NeutralScores()
    {
        return new WaveMetricScores
        {
            roundTime = 50f,
            killCadence = 50f,
            timeToFirstDamage = 50f,
            movement = 50f,
            movementPercentage = 50f,
            damageTaken = 50f,
            damageDealt = 50f,
            points = 50f
        };
    }

    private void EnsureInitialized()
    {
        EnsureWaveDirector();
        if (!initialized)
            ResetToDefaults();
    }

    private void EnsureWaveDirector()
    {
        if (waveDirector != null)
            return;
        ResolveWaveDirector();
    }

    private void ResolveWaveDirector()
    {
        waveDirector = WaveDirector.Instance != null
            ? WaveDirector.Instance
            : FindFirstObjectByType<WaveDirector>();
    }
}
