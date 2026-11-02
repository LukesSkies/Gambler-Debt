using UnityEngine;

/// DATA RETURNED TO THE TEMPO SYSTEM AND DEBUG UI.
///
/// WaveBaselineManager fills this struct with one 0-100 score per metric. The
/// "has..." flags tell TempoDirector/UI whether a real measurement exists. A
/// value of 50 by itself does not mean a measurement exists; always check its
/// matching has... flag before using it in an overall score.
///
/// LIVE CONNECTION: these field names are read by TempoDirector and TempoDebugUI.
/// If a field is renamed here, update both of those scripts as well.
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

/// OWNS THE STARTING EXPECTATIONS AND LEARNED PERFORMANCE BASELINES.
///
/// This is the shared reference point for TempoDirector. It estimates what a
/// wave's workload should look like from the configuration captured by
/// WavePerformanceRecorder, scores the recorded results against the current
/// averages, and learns from a completed wave after TempoDirector has scored it.
///
/// RESPONSIBILITIES:
/// - Keep the solo Round 1 reference workload and performance targets.
/// - Scale expectations to the wave's demon count, health, player count, active
///   demon limit and swipe damage.
/// - Calculate per-metric scores for the live debug UI and completed-wave result.
/// - Gradually learn normalized duration, cadence, first-hit and damage-taken
///   averages from completed waves.
///
/// NOT THIS SCRIPT'S RESPONSIBILITY:
/// - It does not record player/enemy events. WavePerformanceRecorder does that.
/// - It does not decide the overall Tempo Score or intermission. TempoDirector does.
/// - It does not create/spawn a wave. WaveDirector and the production WaveSpawner do.
///
/// LIVE CONNECTIONS REQUIRED:
/// - WavePerformanceRecorder.BeginWave(configuration) must receive the exact
///   WaveConfiguration used by WaveSpawner. That captures the workload fields
///   this script reads from WavePerformanceSnapshot.
/// - WavePerformanceRecorder must publish WaveCompleted only after the whole wave
///   is finished. TempoDirector listens to that event and calls LearnFromCompletedWave.
/// - TempoDirector.GetMetricScores(snapshot) forwards live UI requests here.
/// - RunManager should call TempoDirector.ResetForNewRun, which calls ResetToDefaults.
///
/// The learned averages currently reset for each new run and are not saved between
/// application sessions. Add save/load code here only if cross-session learning is
/// deliberately wanted.
public sealed class WaveBaselineManager : MonoBehaviour
{
    public static WaveBaselineManager Instance { get; private set; }

    [Header("Round 1 Reference Workload")]
    [Tooltip("Number of demons in the default solo Round 1 wave. Keep this aligned with WaveDirector's Round 1 population.")]
    [SerializeField, Min(1)] private int roundOneDemonCount = 6;

    [Tooltip("Reference health for one standard Round 1 demon. Keep this aligned with WaveDirector's roundOneDemonHealth.")]
    [SerializeField, Min(1f)] private float roundOneDemonHealth = 150f;

    [Tooltip("Reference damage from one standard demon swipe. Keep this aligned with WaveDirector's demonSwipeDamage.")]
    [SerializeField, Min(0f)] private float roundOneDemonSwipeDamage = 50f;

    [Tooltip("Reference Round 1 spawn interval. Used only if the Recorder did not capture a real configuration value.")]
    [SerializeField, Min(0.1f)] private float roundOneSpawnInterval = 2f;

    [Tooltip("Reference active demon cap for solo Round 1. Keep this aligned with WaveDirector's baseActiveDemonCap.")]
    [SerializeField, Min(1)] private int roundOneMaximumActiveDemons = 4;

    [Header("Round 1 Performance Defaults")]
    [Tooltip("Initial expected duration for solo Round 1. Later waves are estimated from this using their configured demon workload.")]
    [SerializeField, Min(1f)] private float roundOneExpectedDuration = 40f;

    [Tooltip("Initial expected gap between consecutive kills by the same player in solo Round 1. This is not time to first kill.")]
    [SerializeField, Min(0.1f)] private float roundOneExpectedKillCadence = 5f;

    [Tooltip("Initial expected time from the start of Round 1 until the first successful hit on a demon.")]
    [SerializeField, Min(0.1f)] private float expectedFirstDamageTime = 5f;

    [Tooltip("Starting movement reference shown for diagnostics. Movement scoring itself rewards more movement: 100 percent is always the maximum movement score.")]
    [SerializeField, Range(0f, 100f)] private float expectedMovementPercentage = 60f;

    [Tooltip("Expected total damage received by one player in the solo Round 1 reference workload. This is the base damage-taken threshold before workload scaling.")]
    [SerializeField, Min(0f)] private float roundOneExpectedDamageTaken = 150f;

    [Tooltip("Maximum expected points opportunity per Round 1 reference demon. The supplied example is 8 shots x 10 points + 130 melee points = 210.")]
    [SerializeField, Min(0f)] private float maximumPointsPerRoundOneDemon = 210f;

    [Header("Workload Scaling")]
    [Tooltip("Controls how enemy health changes expected round duration. 0 ignores health, 0.5 uses square-root scaling, and 1 scales linearly.")]
    [SerializeField, Range(0f, 1f)] private float healthWorkloadExponent = 0.5f;

    [Tooltip("Controls how extra players reduce the estimated team round duration. 0.5 uses square-root scaling; 1 uses linear scaling.")]
    [SerializeField, Range(0f, 1f)] private float playerWorkloadExponent = 0.5f;

    [Tooltip("Controls how the total configured enemy-health workload affects expected incoming damage. Actual elapsed round time is deliberately not used for this threshold.")]
    [SerializeField, Range(0f, 1.5f)] private float damageExposureExponent = 0.75f;

    [Tooltip("Controls how configured demon swipe damage changes the expected damage-taken threshold.")]
    [SerializeField, Range(0f, 1.5f)] private float swipeDamageExponent = 0.75f;

    [Header("Learning")]
    [Tooltip("How strongly each completed wave updates the learned ratios. 0.2 means the average moves 20 percent toward the new result.")]
    [SerializeField, Range(0.01f, 1f)] private float learningRate = 0.20f;

    [Tooltip("Limits an individual normalized result so one extreme round cannot completely redefine the learned average.")]
    [SerializeField, Range(1f, 10f)] private float maximumObservedRatio = 4f;

    [Header("Learned Averages (runtime debug values)")]
    [Tooltip("Average observed round duration divided by the workload-estimated duration. 1 means the starting estimate matches the learned average.")]
    [SerializeField, Min(0.01f)] private float averageDurationRatio = 1f;

    [Tooltip("Average observed per-player kill interval divided by the expected interval. Lower actual time is better when scoring.")]
    [SerializeField, Min(0.01f)] private float averageKillCadenceRatio = 1f;

    [Tooltip("Average observed first successful hit time divided by the starting first-hit estimate.")]
    [SerializeField, Min(0.01f)] private float averageFirstDamageRatio = 1f;

    [Tooltip("Average observed damage received divided by the expected damage for the configured workload. Zero-damage waves are not used to reduce this average.")]
    [SerializeField, Min(0.01f)] private float averageDamageTakenRatio = 1f;

    [Tooltip("Average movement percentage for diagnostics only. It is not used to penalize high movement.")]
    [SerializeField, Range(0f, 100f)] private float averageMovementPercentage = 60f;

    [Tooltip("Number of completed waves that have contributed to the learned baseline.")]
    [SerializeField, Min(0)] private int completedWavesLearned;

    // Public read-only values for TempoDirector, debug displays and future tuning tools.
    public int CompletedWavesLearned => completedWavesLearned;
    public float AverageMovementPercentage => averageMovementPercentage;
    public float AverageDurationRatio => averageDurationRatio;
    public float AverageKillCadenceRatio => averageKillCadenceRatio;
    public float AverageFirstDamageRatio => averageFirstDamageRatio;
    public float AverageDamageTakenRatio => averageDamageTakenRatio;

    private bool initialized;

    // Registers the single scene instance, then initializes all learned averages
    // to neutral ratios (1.0) and the initial Round 1 movement reference.
    //
    // LIVE SCENE SETUP: put one WaveBaselineManager in the gameplay scene. If a
    // persistent RunManager owns managers across scene changes, keep this object
    // with that manager rather than creating another copy in each scene.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple WaveBaselineManager instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResetToDefaults();
    }

    // Clears the singleton reference when this manager is destroyed.
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Clamps Inspector values to valid ranges. This runs in the editor when
    // serialized values change; it is not the runtime learning calculation.
    // Keep the Round 1 workload values synchronized with WaveDirector.
    private void OnValidate()
    {
        roundOneDemonCount = Mathf.Max(1, roundOneDemonCount);
        roundOneDemonHealth = Mathf.Max(1f, roundOneDemonHealth);
        roundOneSpawnInterval = Mathf.Max(0.1f, roundOneSpawnInterval);
        roundOneMaximumActiveDemons = Mathf.Max(1, roundOneMaximumActiveDemons);
        roundOneExpectedDuration = Mathf.Max(1f, roundOneExpectedDuration);
        roundOneExpectedDamageTaken = Mathf.Max(0f, roundOneExpectedDamageTaken);
        maximumPointsPerRoundOneDemon = Mathf.Max(0f, maximumPointsPerRoundOneDemon);
        roundOneExpectedKillCadence = Mathf.Max(0.1f, roundOneExpectedKillCadence);
        learningRate = Mathf.Clamp(learningRate, 0.01f, 1f);
        maximumObservedRatio = Mathf.Max(1f, maximumObservedRatio);
    }

    // Resets the learned values to the starting Round 1 references. Call this
    // once when a new run begins, not when each new wave begins; otherwise the
    // manager would forget everything it learned from the previous wave.
    //
    // LIVE CONNECTION: TempoDirector.ResetForNewRun already calls this method.
    // The real RunManager should call TempoDirector.ResetForNewRun when a new
    // run is created/restarted.
    public void ResetToDefaults()
    {
        averageDurationRatio = 1f;
        averageKillCadenceRatio = 1f;
        averageFirstDamageRatio = 1f;
        averageDamageTakenRatio = 1f;
        averageMovementPercentage = expectedMovementPercentage;
        completedWavesLearned = 0;
        initialized = true;
    }

    // Calculates the current scores from a Recorder snapshot without changing
    // any average. TempoDebugUI calls this through TempoDirector while the wave
    // is active, so the display can update live without teaching the baseline
    // from an unfinished wave.
    //
    // LIVE DATA REQUIRED: snapshot fields come from WavePerformanceRecorder:
    // duration, damage totals, points, first-hit time, kill cadence, movement
    // percentage, player count and the captured WaveConfiguration values.
    public WaveMetricScores GetMetricScores(WavePerformanceSnapshot snapshot)
    {
        EnsureInitialized();

        WaveMetricScores scores = NeutralScores();
        if (snapshot == null)
            return scores;

        float expectedDuration = EstimateExpectedRoundDuration(snapshot);
        float expectedKillCadence = EstimateExpectedKillCadence(snapshot);
        float expectedDamageTaken = EstimateExpectedDamageTaken(snapshot);
        float expectedTotalEnemyHealth = EstimateExpectedTotalEnemyHealth(snapshot);
        float expectedMaximumPoints = EstimateExpectedMaximumPoints(snapshot);
        float expectedFirstHit = EstimateExpectedFirstDamageTime(snapshot);

        // ROUND TIME: compare actual elapsed time with the expected duration for
        // the current population/health/player workload. The learned duration ratio
        // gradually adjusts the estimate to match actual playtest behaviour.
        scores.hasRoundTime = snapshot.duration > 0f && expectedDuration > 0f;
        if (scores.hasRoundTime)
        {
            float normalizedDuration = SafeRatio(snapshot.duration, expectedDuration);
            scores.roundTime = ScoreLowerIsBetter(normalizedDuration, averageDurationRatio);
        }

        // KILL CADENCE: Recorder stores an average of each player's own consecutive
        // kill intervals. It is not the interval between alternating team kills.
        // A player needs at least two confirmed kills before a cadence exists.
        scores.hasKillCadence = snapshot.hasKillCadence && expectedKillCadence > 0f;
        if (scores.hasKillCadence)
        {
            float normalizedCadence = SafeRatio(
                snapshot.averageTimeBetweenKills,
                expectedKillCadence);

            scores.killCadence = ScoreLowerIsBetter(
                normalizedCadence,
                averageKillCadenceRatio);
        }

        // TIME TO FIRST DAMAGE: measures time from BeginWave to the first successful
        // hit on an enemy. Until that hit occurs, the live UI uses snapshot.duration
        // as the running timer. At wave end, an untriggered first hit is unavailable
        // and TempoDirector excludes it from the completed-wave weighted score.
        float observedFirstHitTime = snapshot.hasFirstDamage
            ? snapshot.firstDamageTime
            : snapshot.duration;

        scores.hasFirstDamage = snapshot.hasFirstDamage;

        if (expectedFirstHit > 0f)
        {
            float normalizedFirstHit = SafeRatio(
                observedFirstHitTime,
                expectedFirstHit);

            scores.timeToFirstDamage = ScoreLowerIsBetter(
                normalizedFirstHit,
                averageFirstDamageRatio);
        }

        // MOVEMENT: higher movement is intentionally better in this game. The score
        // is the reported percentage itself: 0% = 0, 50% = 50, 100% = 100. The two
        // UI fields share this same result and TempoDirector counts it only once.
        // Recorder.RecordMovement must be called every frame for every player,
        // including while standing still, or the percentage is not trustworthy.
        scores.hasMovementSamples = snapshot.activeCombatTime > 0f;
        if (scores.hasMovementSamples)
        {
            float movementScore = Mathf.Clamp(
                snapshot.movementPercentage,
                0f,
                100f);

            scores.movement = movementScore;
            scores.movementPercentage = movementScore;
        }

        // DAMAGE TAKEN: use the full-wave expected amount. Do not divide the target
        // by elapsed time; doing so made the same damage total change colour merely
        // because the round was taking longer. Expected damage is workload-scaled
        // from configured demons/health, player count, swipe damage and active cap.
        // Zero received damage always returns a perfect score. The learned ratio
        // adapts the full-wave target only after completed waves with actual damage.
        scores.hasDamageTaken = snapshot.duration >= 0f;
        if (scores.hasDamageTaken)
        {
            float learnedExpectedDamage = expectedDamageTaken * averageDamageTakenRatio;
            scores.damageTaken = ScoreDamageTaken(
                snapshot.damageTaken,
                learnedExpectedDamage);
        }

        // DAMAGE DEALT: this is total recorded enemy health removed divided by the
        // total health workload (demon count * health). It is not divided by time.
        // For a fully cleared wave, this will normally reach 100 because the team
        // has removed all scheduled enemy health. Therefore it is an opportunity/
        // completion measure, not a useful speed measure at wave end. Round Time
        // and Kill Cadence are the metrics that distinguish faster clears.
        //
        // LIVE CONNECTION: the demon health/damage system must call Recorder's
        // RecordDamageDealt only for health actually removed. Avoid recording
        // overkill damage above a demon's remaining health, or this percentage can
        // reach 100 before all enemies are defeated.
        scores.hasDamageDealt = expectedTotalEnemyHealth > 0f;
        if (scores.hasDamageDealt)
        {
            float damageCompletionRatio = SafeRatio(
                snapshot.damageDealt,
                expectedTotalEnemyHealth);

            scores.damageDealt = Mathf.Clamp01(damageCompletionRatio) * 100f;
        }

        // POINTS: compare total points with the maximum expected points opportunity,
        // not points per second. With the current Round 1 reference this is
        // 6 demons * 210 points = 1,260. An identical points total gets the same
        // score regardless of round duration.
        //
        // The health-based increase below assumes more demon health permits more
        // point-awarding shots. If the live scoring/economy script awards a fixed
        // maximum per demon regardless of health, change EstimateExpectedMaximumPoints
        // to use a fixed per-demon amount instead of multiplying by health ratio.
        scores.hasPoints = expectedMaximumPoints > 0f;
        if (scores.hasPoints)
        {
            float pointsOpportunityRatio = SafeRatio(
                snapshot.pointsEarned,
                expectedMaximumPoints);

            scores.points = Mathf.Clamp01(pointsOpportunityRatio) * 100f;
        }

        return scores;
    }

    // Updates learned averages from one finished wave. TempoDirector must call
    // this AFTER it calculates the completed wave's metric scores and overall
    // Tempo Score. That means a wave is judged against the previous baseline and
    // cannot change its own target before the comparison is made.
    //
    // Only metrics with valid measurements are learned. In particular, zero
    // damage-taken rounds do not pull the damage expectation down toward zero.
    // Damage dealt and points use fixed workload opportunity ceilings and are
    // deliberately not converted into per-second averages.
    public void LearnFromCompletedWave(WavePerformanceSnapshot snapshot)
    {
        EnsureInitialized();
        if (snapshot == null)
            return;

        float expectedDuration = EstimateExpectedRoundDuration(snapshot);
        float expectedKillCadence = EstimateExpectedKillCadence(snapshot);
        float expectedDamageTaken = EstimateExpectedDamageTaken(snapshot);
        float expectedFirstHit = EstimateExpectedFirstDamageTime(snapshot);

        if (snapshot.duration > 0f && expectedDuration > 0f)
        {
            LearnRatio(
                ref averageDurationRatio,
                snapshot.duration / expectedDuration);
        }

        if (snapshot.hasKillCadence && expectedKillCadence > 0f)
        {
            LearnRatio(
                ref averageKillCadenceRatio,
                snapshot.averageTimeBetweenKills / expectedKillCadence);
        }

        if (snapshot.hasFirstDamage && expectedFirstHit > 0f)
        {
            LearnRatio(
                ref averageFirstDamageRatio,
                snapshot.firstDamageTime / expectedFirstHit);
        }

        // Learn incoming damage only from completed waves where damage was really
        // received. A zero-damage result is excellent, but treating zero as an
        // ordinary ratio would make the expected-damage threshold collapse over time.
        if (snapshot.isComplete &&
            snapshot.damageTaken > 0f &&
            expectedDamageTaken > 0f)
        {
            LearnRatio(
                ref averageDamageTakenRatio,
                snapshot.damageTaken / expectedDamageTaken);
        }

        // The movement average is retained for diagnostics/balancing only. It is
        // not used to penalize a player who moves above the average.
        if (snapshot.activeCombatTime > 0f)
        {
            averageMovementPercentage = Mathf.Lerp(
                averageMovementPercentage,
                Mathf.Clamp(snapshot.movementPercentage, 0f, 100f),
                learningRate);
        }

        completedWavesLearned++;
    }

    // Estimates how long a wave should take from the captured workload. More
    // demons and higher per-demon health increase expected work; more players
    // reduce the team estimate using the configured player exponent. The spawn
    // schedule is also used as a minimum duration estimate.
    //
    // LIVE CONNECTION: WavePerformanceRecorder.BeginWave(configuration) copies
    // demon count, health, spawn interval and batch size into the snapshot. If
    // the Recorder is started using only the fallback int overload, this method
    // has to estimate missing values and will not know the exact planned workload.
    public float EstimateExpectedRoundDuration(WavePerformanceSnapshot snapshot)
    {
        if (snapshot == null)
            return roundOneExpectedDuration;

        int players = Mathf.Clamp(snapshot.playerCount, 1, 4);
        int demons = GetExpectedDemonCount(snapshot);
        float health = GetExpectedDemonHealth(snapshot);

        float healthRatio = Mathf.Max(
            0.01f,
            health / roundOneDemonHealth);

        float healthScale = Mathf.Pow(
            healthRatio,
            healthWorkloadExponent);

        float playerScale = Mathf.Pow(
            players,
            playerWorkloadExponent);

        float workloadDuration = roundOneExpectedDuration
            * (demons / (float)roundOneDemonCount)
            * healthScale
            / Mathf.Max(1f, playerScale);

        int batchSize = Mathf.Max(1, snapshot.configuredSpawnBatchLimit);
        int batchCount = Mathf.CeilToInt(demons / (float)batchSize);

        float spawnInterval = snapshot.configuredSpawnInterval > 0f
            ? snapshot.configuredSpawnInterval
            : roundOneSpawnInterval;

        float spawnScheduleDuration = Mathf.Max(0, batchCount - 1) * spawnInterval;

        return Mathf.Max(
            1f,
            Mathf.Max(workloadDuration, spawnScheduleDuration));
    }

    // Estimates the expected interval between consecutive kills by one player.
    // Higher demon health increases the expected interval. The active demon cap
    // changes how many targets may be available at once, and is divided by player
    // count because the Recorder calculates a per-player cadence.
    public float EstimateExpectedKillCadence(WavePerformanceSnapshot snapshot)
    {
        float health = GetExpectedDemonHealth(snapshot);
        float healthRatio = Mathf.Max(0.01f, health / roundOneDemonHealth);
        float healthScale = Mathf.Pow(healthRatio, healthWorkloadExponent);

        int players = Mathf.Clamp(snapshot.playerCount, 1, 4);
        int activeCap = GetExpectedActiveCap(snapshot);
        float activeCapPerPlayer = activeCap / (float)players;

        float targetAvailabilityScale = Mathf.Sqrt(
            roundOneMaximumActiveDemons / Mathf.Max(0.5f, activeCapPerPlayer));

        targetAvailabilityScale = Mathf.Clamp(targetAvailabilityScale, 0.5f, 2f);

        return Mathf.Max(
            0.1f,
            roundOneExpectedKillCadence * healthScale * targetAvailabilityScale);
    }

    // Estimates how quickly the team should land its first hit. Additional players
    // create more opportunities for an early hit. This is a heuristic starting
    // target; tune it with recorded matches once the real spawn/player flow exists.
    private float EstimateExpectedFirstDamageTime(WavePerformanceSnapshot snapshot)
    {
        int players = Mathf.Clamp(snapshot.playerCount, 1, 4);
        return Mathf.Max(
            0.1f,
            expectedFirstDamageTime / Mathf.Sqrt(players));
    }

    // Estimates how much damage the team might reasonably receive during the
    // full wave. The expectation uses workload, not actual elapsed duration:
    // configured demon count and health estimate the amount of enemy pressure,
    // swipe damage changes the severity of each hit, active cap estimates how many
    // enemies can threaten players simultaneously, and player count scales the
    // team total. Tune these exponents against real matches; they are a model, not
    // a guarantee of how much damage players must take.
    private float EstimateExpectedDamageTaken(WavePerformanceSnapshot snapshot)
    {
        int players = Mathf.Clamp(snapshot.playerCount, 1, 4);
        int demons = GetExpectedDemonCount(snapshot);
        float demonHealth = GetExpectedDemonHealth(snapshot);

        float populationHealthRatio =
            (demons / (float)roundOneDemonCount) *
            Mathf.Max(0.01f, demonHealth / roundOneDemonHealth);

        float workloadScale = Mathf.Pow(
            Mathf.Max(0.1f, populationHealthRatio),
            damageExposureExponent);

        float swipeDamage = snapshot.configuredDemonSwipeDamage > 0f
            ? snapshot.configuredDemonSwipeDamage
            : roundOneDemonSwipeDamage;

        float swipeScale = roundOneDemonSwipeDamage > 0f
            ? Mathf.Pow(
                Mathf.Max(0.01f, swipeDamage / roundOneDemonSwipeDamage),
                swipeDamageExponent)
            : 1f;

        int activeCap = GetExpectedActiveCap(snapshot);
        float activeCapScale = Mathf.Sqrt(
            activeCap / (float)roundOneMaximumActiveDemons);

        return roundOneExpectedDamageTaken
            * players
            * workloadScale
            * swipeScale
            * activeCapScale;
    }

    // Calculates all scheduled enemy health. A real WaveSpawner/health system
    // should make sure the Recorder is given the same count and health that the
    // demons actually receive.
    private float EstimateExpectedTotalEnemyHealth(WavePerformanceSnapshot snapshot)
    {
        return GetExpectedDemonCount(snapshot) * GetExpectedDemonHealth(snapshot);
    }

    // Estimates the maximum points opportunity for the configured wave. The
    // current formula uses 210 points per Round 1 demon and scales linearly with
    // enemy health, assuming extra health permits additional point-awarding hits.
    //
    // LIVE GAME CHANGE: connect this formula to the actual points/economy rules.
    // If the live game always caps each demon at 210 points regardless of health,
    // remove the healthRatio multiplier. If point values change by weapon, body
    // part, melee kill or difficulty, calculate the true maximum from those rules.
    private float EstimateExpectedMaximumPoints(WavePerformanceSnapshot snapshot)
    {
        float health = GetExpectedDemonHealth(snapshot);
        float healthRatio = Mathf.Max(0.01f, health / roundOneDemonHealth);

        return maximumPointsPerRoundOneDemon
            * GetExpectedDemonCount(snapshot)
            * healthRatio;
    }

    // Uses the actual configured demon count saved by the Recorder. If the value
    // is zero, the wave was likely started through the temporary/test overload
    // instead of BeginWave(WaveConfiguration), so a basic fallback is used.
    // Replace the fallback growth numbers if WaveDirector's population formula changes.
    private int GetExpectedDemonCount(WavePerformanceSnapshot snapshot)
    {
        if (snapshot.configuredDemonCount > 0)
            return snapshot.configuredDemonCount;

        return Mathf.Max(
            1,
            roundOneDemonCount
            + (Mathf.Max(1, snapshot.roundNumber) - 1) * 2
            + (Mathf.Clamp(snapshot.playerCount, 1, 4) - 1) * 2);
    }

    // Uses the per-demon health captured from WaveConfiguration. If it was not
    // captured, this fallback mirrors the current supplied WaveDirector's health
    // progression. Keep this fallback synchronized if that progression changes.
    private float GetExpectedDemonHealth(WavePerformanceSnapshot snapshot)
    {
        if (snapshot.configuredDemonHealth > 0f)
            return snapshot.configuredDemonHealth;

        int round = Mathf.Max(1, snapshot.roundNumber);

        if (round <= 9)
            return roundOneDemonHealth + (round - 1) * 100f;

        float roundNineHealth = roundOneDemonHealth + 8f * 100f;
        return roundNineHealth * Mathf.Pow(1.1f, round - 9);
    }

    // Reads the maximum active demon count stored by Recorder. When it is missing,
    // the solo Round 1 cap is used. Production waves should always pass a real config.
    private int GetExpectedActiveCap(WavePerformanceSnapshot snapshot)
    {
        return snapshot.configuredMaximumActiveDemons > 0
            ? snapshot.configuredMaximumActiveDemons
            : roundOneMaximumActiveDemons;
    }

    // Moves one learned ratio toward the latest observed ratio using the Inspector
    // learning rate. Extreme observations are clamped to prevent one unusual wave
    // from changing the baseline too aggressively.
    private void LearnRatio(ref float average, float observedRatio)
    {
        float minimum = 1f / maximumObservedRatio;
        float clamped = Mathf.Clamp(
            observedRatio,
            minimum,
            maximumObservedRatio);

        average = Mathf.Max(
            0.01f,
            Mathf.Lerp(average, clamped, learningRate));
    }

    // Returns actual divided by expected, with a neutral fallback for invalid expectations.
    private static float SafeRatio(float actual, float expected)
    {
        if (expected <= 0f)
            return 1f;

        return Mathf.Max(0f, actual) / expected;
    }

    // Converts incoming damage into a score using the full expected amount.
    // For an expected 150 damage: 0 damage = 100; 100 damage is about 67;
    // 150 damage is 49; 200 damage is about 33; 300 damage is 0.
    // The UI then maps these scores to green/yellow/red/orange.
    private static float ScoreDamageTaken(float actualDamage, float expectedDamage)
    {
        actualDamage = Mathf.Max(0f, actualDamage);

        if (actualDamage <= 0f)
            return 100f;

        if (expectedDamage <= 0f)
            return 0f;

        float ratio = actualDamage / expectedDamage;

        if (ratio < 1f)
        {
            return Mathf.Lerp(
                100f,
                50f,
                Mathf.Clamp01(ratio));
        }

        return Mathf.Lerp(
            49f,
            0f,
            Mathf.Clamp01(ratio - 1f));
    }

    // Scores a lower-is-better result such as round duration or kill interval.
    // The caller passes normalized ratios, not raw seconds. Faster than the learned
    // reference scores higher; slower than the reference scores lower.
    private static float ScoreLowerIsBetter(float actualRatio, float averageRatio)
    {
        if (averageRatio <= 0f)
            return 50f;

        return Mathf.Clamp(
            100f - (Mathf.Max(0f, actualRatio) / averageRatio) * 50f,
            0f,
            100f);
    }

    // Returns neutral per-metric values when there is no snapshot to score. The
    // availability flags remain false, so TempoDirector will not add these values
    // to its weighted completed-wave score.
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

    // Safety initialization in case another component calls a public method
    // before Awake has initialized the baseline. Normally Awake calls ResetToDefaults.
    private void EnsureInitialized()
    {
        if (!initialized)
            ResetToDefaults();
    }
}
