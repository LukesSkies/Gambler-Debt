using UnityEngine;

/// EVALUATES COMPLETED WAVES AND CHOOSES THE NEXT INTERMISSION.
///
/// This component receives the finished snapshot from WavePerformanceRecorder,
/// asks WaveBaselineManager to score it, combines the metric scores into one
/// Tempo Score, and calculates how long the next break should be.
///
/// RESPONSIBILITIES:
/// - Subscribe to WavePerformanceRecorder.WaveCompleted.
/// - Ask WaveBaselineManager for the metric scores against the OLD baseline.
/// - Combine available metric scores using the weights below.
/// - Optionally smooth the overall score against the previous wave.
/// - Calculate NextIntermissionSeconds for the production WaveManager.
/// - Tell WaveBaselineManager to learn from the wave AFTER scoring it.
///
/// LIVE GAME CONNECTIONS:
/// - The supplied WavePerformanceRecorder already exposes WaveCompleted and
///   publishes it from EndWave(). It should be ended by the real WaveManager only
///   after every demon scheduled for that wave has been defeated.
/// - The supplied WaveDirector reads TempoScore when it creates a configuration.
///   The real WaveManager should call WaveDirector.PreviewWave (or a future
///   production-named configuration method) for the next round.
/// - The production WaveManager must use NextIntermissionSeconds when timing the
///   break between completed waves and the next wave start.
/// - The real RunManager should call ResetForNewRun once at the beginning of a
///   fresh run, not between individual rounds.
///
/// This component does not spawn demons, measure player movement, award points,
/// apply damage, or control the intermission timer itself.
public sealed class TempoDirector : MonoBehaviour
{
    public static TempoDirector Instance { get; private set; }

    [Header("References")]
    [Tooltip("The central owner of Round 1 expectations and learned averages. Assign the scene instance here or allow auto-discovery.")]
    [SerializeField] private WaveBaselineManager baselineManager;

    [Tooltip("The Recorder that publishes completed wave snapshots. Assign the scene instance here or allow auto-discovery.")]
    [SerializeField] private WavePerformanceRecorder recorder;

    [Header("Intermission (seconds)")]
    [Tooltip("Shortest break before the next wave when the Tempo Score is 100.")]
    [SerializeField, Min(0f)] private float minimumIntermission = 5f;

    [Tooltip("Break used when the Tempo Score is neutral at 50.")]
    [SerializeField, Min(0f)] private float defaultIntermission = 12f;

    [Tooltip("Longest break before the next wave when the Tempo Score is 0.")]
    [SerializeField, Min(0f)] private float maximumIntermission = 25f;

    [Header("Score Smoothing")]
    [Tooltip("Blends the new completed-wave score with the previous smoothed score. Disable to use each wave's raw score directly.")]
    [SerializeField] private bool useScoreSmoothing = true;

    [Range(0f, 1f)]
    [Tooltip("How much of the previous smoothed score to retain. 0.25 means 75 percent current score and 25 percent previous score.")]
    [SerializeField] private float previousScoreInfluence = 0.25f;

    [Header("Latest Results (debug values)")]
    [Tooltip("Latest overall Tempo Score from 0 to 100. WaveDirector reads this when planning the next wave.")]
    [SerializeField, Range(0f, 100f)] private float tempoScore = 50f;

    [Tooltip("Intermission duration calculated from the latest Tempo Score. The production WaveManager should wait this many seconds before starting the next wave.")]
    [SerializeField, Min(0f)] private float nextIntermissionSeconds = 12f;

    [Tooltip("Expected duration estimated for the most recently evaluated wave. This is a workload-based reference, not the actual round timer.")]
    [SerializeField, Min(0f)] private float lastExpectedRoundDuration;

    [Tooltip("Expected average gap between each player's consecutive kills for the most recently evaluated wave.")]
    [SerializeField, Min(0f)] private float lastExpectedKillCadence;

    [Tooltip("Number of completed wave snapshots this TempoDirector has evaluated during the current run.")]
    [SerializeField, Min(0)] private int completedWavesEvaluated;

    // These private values store only Tempo Score smoothing history. The detailed
    // metric averages live in WaveBaselineManager, not here.
    private float previousSmoothedScore = 50f;
    private bool hasPreviousScore;

    // Kept so this script can unsubscribe from the exact Recorder it subscribed to.
    private WavePerformanceRecorder subscribedRecorder;

    // Public read-only interface used by WaveDirector, WaveManager and debug UI.
    public float TempoScore => tempoScore;
    public float NextIntermissionSeconds => nextIntermissionSeconds;
    public float LastExpectedRoundDuration => lastExpectedRoundDuration;
    public float LastExpectedKillCadence => lastExpectedKillCadence;
    public int CompletedWavesEvaluated => completedWavesEvaluated;
    public WaveBaselineManager BaselineManager => baselineManager;

    // Registers this as the one active TempoDirector and attempts to find the
    // Baseline Manager. The singleton is used by the supplied WaveDirector and
    // debug scripts to locate the TempoDirector.
    //
    // LIVE SCENE SETUP: place one TempoDirector in the gameplay scene. If the
    // project later uses explicit dependency injection or multiple game sessions,
    // replace singleton lookups consistently with references supplied by RunManager.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple TempoDirector instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FindBaselineManager();
    }

    // Finds the baseline and subscribes to the Recorder when enabled.
    private void OnEnable()
    {
        FindBaselineManager();
        SubscribeToRecorder();
    }

    // Retries lookups after all scene Awake calls have run. This handles the case
    // where component initialization order meant the singleton was not available
    // during OnEnable.
    private void Start()
    {
        FindBaselineManager();
        SubscribeToRecorder();
    }

    // Fallback for a Recorder or Baseline Manager created later by a scene loader.
    // The scoring itself is event-driven; this Update does not evaluate metrics.
    // If all managers are guaranteed to exist from scene load, the repeated null
    // checks can be removed and references assigned directly in the Inspector.
    private void Update()
    {
        if (baselineManager == null)
            FindBaselineManager();

        if (subscribedRecorder == null)
            SubscribeToRecorder();
    }

    // Stops listening while disabled so completed waves are not evaluated unexpectedly.
    private void OnDisable()
    {
        UnsubscribeFromRecorder();
    }

    // Unsubscribes and clears the singleton if this is the active instance.
    private void OnDestroy()
    {
        UnsubscribeFromRecorder();

        if (Instance == this)
            Instance = null;
    }

    // Keeps the intermission values and smoothing setting inside safe Inspector
    // limits. No gameplay events are recorded here.
    private void OnValidate()
    {
        minimumIntermission = Mathf.Max(0f, minimumIntermission);
        maximumIntermission = Mathf.Max(minimumIntermission, maximumIntermission);
        defaultIntermission = Mathf.Clamp(
            defaultIntermission,
            minimumIntermission,
            maximumIntermission);

        previousScoreInfluence = Mathf.Clamp01(previousScoreInfluence);
    }

    // Finds the single WaveBaselineManager in the scene. That manager stores the
    // learned averages and performs all per-metric comparisons.
    private void FindBaselineManager()
    {
        if (baselineManager == null)
        {
            baselineManager = WaveBaselineManager.Instance != null
                ? WaveBaselineManager.Instance
                : FindFirstObjectByType<WaveBaselineManager>();
        }
    }

    // Finds the Recorder and subscribes to its WaveCompleted event. The supplied
    // Recorder raises that event from EndWave after creating a final snapshot.
    // The event is the live connection between recording and evaluation.
    //
    // LIVE GAME CHANGE: ensure the production WaveManager calls Recorder.EndWave
    // exactly once when the wave is genuinely complete. Do not also call
    // EvaluateCompletedWave manually from WaveManager, or the same wave could be
    // scored twice.
    private void SubscribeToRecorder()
    {
        if (recorder == null)
        {
            recorder = WavePerformanceRecorder.Instance != null
                ? WavePerformanceRecorder.Instance
                : FindFirstObjectByType<WavePerformanceRecorder>();
        }

        if (recorder == null || recorder == subscribedRecorder)
            return;

        UnsubscribeFromRecorder();
        subscribedRecorder = recorder;
        subscribedRecorder.WaveCompleted += EvaluateCompletedWave;
    }

    // Removes the event subscription from the Recorder currently stored in subscribedRecorder.
    private void UnsubscribeFromRecorder()
    {
        if (subscribedRecorder != null)
            subscribedRecorder.WaveCompleted -= EvaluateCompletedWave;

        subscribedRecorder = null;
    }

    // Public read-only scoring method used by TempoDebugUI to get live metric
    // colours. It forwards the snapshot to WaveBaselineManager and does not
    // update learned averages or the overall Tempo Score.
    //
    // LIVE CONNECTION: TempoDebugUI obtains the current snapshot from
    // WavePerformanceRecorder.GetCurrentSnapshot and passes it into this method.
    public WaveMetricScores GetMetricScores(WavePerformanceSnapshot snapshot)
    {
        FindBaselineManager();

        if (baselineManager != null)
            return baselineManager.GetMetricScores(snapshot);

        return CreateNeutralScores();
    }

    // Evaluates a finished wave. Normally this is called automatically by the
    // Recorder's WaveCompleted event, not by Update and not directly by the HUD.
    //
    // EVALUATION ORDER:
    // 1. Read expected duration/cadence and scores from the current baseline.
    // 2. Combine available scores using the weights below.
    // 3. Apply optional smoothing and calculate the next intermission.
    // 4. Only then update the baseline with this wave's results.
    //
    // This ordering prevents the current wave from changing the average it is
    // being judged against.
    public void EvaluateCompletedWave(WavePerformanceSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        FindBaselineManager();

        if (baselineManager == null)
        {
            Debug.LogWarning("TempoDirector cannot evaluate the wave because WaveBaselineManager is missing.");
            return;
        }

        // Save the workload expectations for Inspector/debug inspection.
        lastExpectedRoundDuration = baselineManager.EstimateExpectedRoundDuration(snapshot);
        lastExpectedKillCadence = baselineManager.EstimateExpectedKillCadence(snapshot);

        // Calculate each metric score against the averages BEFORE learning this wave.
        WaveMetricScores metrics = baselineManager.GetMetricScores(snapshot);

        float weightedScore = 0f;
        float totalWeight = 0f;

        // These weights determine the influence of each metric on the overall score.
        // Movement Time and Movement Percentage display separately but use the same
        // movement measurement, so they are deliberately counted only once.
        AddMetric(metrics.roundTime, 0.25f, ref weightedScore, ref totalWeight, metrics.hasRoundTime);
        AddMetric(metrics.killCadence, 0.30f, ref weightedScore, ref totalWeight, metrics.hasKillCadence);
        AddMetric(metrics.timeToFirstDamage, 0.05f, ref weightedScore, ref totalWeight, metrics.hasFirstDamage);
        AddMetric(metrics.movementPercentage, 0.05f, ref weightedScore, ref totalWeight, metrics.hasMovementSamples);
        AddMetric(metrics.damageTaken, 0.15f, ref weightedScore, ref totalWeight, metrics.hasDamageTaken);
        AddMetric(metrics.damageDealt, 0.10f, ref weightedScore, ref totalWeight, metrics.hasDamageDealt);
        AddMetric(metrics.points, 0.10f, ref weightedScore, ref totalWeight, metrics.hasPoints);

        // Missing metrics are skipped and the remaining weights are normalized.
        // If no valid metrics exist, use a neutral score of 50 rather than treating
        // missing data as either perfect or terrible performance.
        float rawScore = totalWeight > 0f
            ? weightedScore / totalWeight
            : 50f;

        rawScore = Mathf.Clamp(rawScore, 0f, 100f);

        // Optional smoothing retains some previous score to avoid large difficulty
        // swings from one unusual wave. It affects the overall score only; the
        // Baseline Manager still learns from the current wave's actual measurements.
        tempoScore = useScoreSmoothing && hasPreviousScore
            ? Mathf.Lerp(rawScore, previousSmoothedScore, previousScoreInfluence)
            : rawScore;

        previousSmoothedScore = tempoScore;
        hasPreviousScore = true;

        // This is only a calculated duration. The production WaveManager must run
        // the actual timer/coroutine using NextIntermissionSeconds.
        nextIntermissionSeconds = CalculateIntermission(tempoScore);
        completedWavesEvaluated++;

        // LIVE CONNECTION: WaveBaselineManager now learns from the completed
        // snapshot, but only after this wave has been scored against the old values.
        baselineManager.LearnFromCompletedWave(snapshot);

        Debug.Log(
            $"Tempo Director: Round {snapshot.roundNumber} complete. " +
            $"Duration score {metrics.roundTime:F0}, " +
            $"per-player kill cadence {(metrics.hasKillCadence ? metrics.killCadence.ToString("F0") : "n/a")}, " +
            $"first damage {(metrics.hasFirstDamage ? metrics.timeToFirstDamage.ToString("F0") : "n/a")}, " +
            $"damage taken {metrics.damageTaken:F0}, damage dealt {metrics.damageDealt:F0}, points {metrics.points:F0}, " +
            $"overall {tempoScore:F1}/100; next intermission {nextIntermissionSeconds:F1}s; " +
            $"baseline waves learned {baselineManager.CompletedWavesLearned}.");
    }

    // Converts the overall Tempo Score into a break length. A score of 0 gives
    // maximumIntermission, 50 gives defaultIntermission, and 100 gives
    // minimumIntermission. Values between those points are interpolated.
    //
    // LIVE GAME CHANGE: the real WaveManager must read NextIntermissionSeconds
    // after the wave is complete and wait that long before starting the next one.
    private float CalculateIntermission(float score)
    {
        if (score < 50f)
        {
            float t = Mathf.InverseLerp(0f, 50f, score);
            return Mathf.Lerp(maximumIntermission, defaultIntermission, t);
        }

        float highT = Mathf.InverseLerp(50f, 100f, score);
        return Mathf.Lerp(defaultIntermission, minimumIntermission, highT);
    }

    // Resets Tempo's smoothing history and asks WaveBaselineManager to restore
    // the initial Round 1 averages. Call only when starting/restarting a run.
    // Do not call this when moving from one normal round to the next.
    //
    // LIVE CONNECTION: call this from the production RunManager's new-run/reset
    // method, alongside resetting the real round number and Recorder state.
    public void ResetForNewRun()
    {
        FindBaselineManager();

        if (baselineManager != null)
            baselineManager.ResetToDefaults();

        tempoScore = 50f;
        nextIntermissionSeconds = defaultIntermission;
        lastExpectedRoundDuration = 0f;
        lastExpectedKillCadence = 0f;
        completedWavesEvaluated = 0;
        previousSmoothedScore = 50f;
        hasPreviousScore = false;
    }

    // Adds one available metric to the weighted total. A false availability flag
    // means the Recorder did not supply a valid sample for that metric, so its
    // weight is not included in the final normalization.
    private static void AddMetric(
        float score,
        float weight,
        ref float weightedScore,
        ref float totalWeight,
        bool available)
    {
        if (!available || weight <= 0f)
            return;

        weightedScore += Mathf.Clamp(score, 0f, 100f) * weight;
        totalWeight += weight;
    }

    // Returns neutral placeholders if the baseline manager is unavailable.
    // The availability flags remain false, so a completed wave cannot accidentally
    // be scored as if all eight measurements were valid.
    private static WaveMetricScores CreateNeutralScores()
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
}
