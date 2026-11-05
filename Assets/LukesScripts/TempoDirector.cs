using UnityEngine;

[DisallowMultipleComponent]
public sealed class TempoDirector : MonoBehaviour
{
    public static TempoDirector Instance { get; private set; }

    private WaveDirector waveDirector;
    private WaveBaselineManager baselineManager;
    private WavePerformanceRecorder recorder;
    private WavePerformanceRecorder subscribedRecorder;

    // Runtime output. Not serialized: these are results, not Inspector settings.
    private float tempoScore = 50f;
    private float nextIntermissionSeconds = 12f;
    private float lastExpectedRoundDuration;
    private float lastExpectedKillCadence;
    private int completedWavesEvaluated;
    private float previousSmoothedScore = 50f;
    private bool hasPreviousScore;

    public float TempoScore => tempoScore;
    public float NextIntermissionSeconds => nextIntermissionSeconds;
    public float LastExpectedRoundDuration => lastExpectedRoundDuration;
    public float LastExpectedKillCadence => lastExpectedKillCadence;
    public float LastExpectedKillInterval => lastExpectedKillCadence;
    public float ExpectedFirstDamageTime => waveDirector != null ? waveDirector.ExpectedFirstDamageTime : 5f;
    public int CompletedWavesEvaluated => completedWavesEvaluated;
    public WaveBaselineManager BaselineManager => baselineManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple TempoDirector instances detected. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToRecorder();
    }

    private void Start()
    {
        ResolveReferences();
        SubscribeToRecorder();
        nextIntermissionSeconds = waveDirector != null ? waveDirector.DefaultIntermission : 12f;
    }

    private void Update()
    {
        // Supports scene objects that initialise after this component.
        if (waveDirector == null || baselineManager == null || recorder == null)
            ResolveReferences();
        if (subscribedRecorder == null)
            SubscribeToRecorder();
    }

    private void OnDisable()
    {
        UnsubscribeFromRecorder();
    }

    private void OnDestroy()
    {
        UnsubscribeFromRecorder();
        if (Instance == this)
            Instance = null;
    }

    private void ResolveReferences()
    {
        if (waveDirector == null)
        {
            waveDirector = WaveDirector.Instance != null
                ? WaveDirector.Instance
                : FindFirstObjectByType<WaveDirector>();
        }

        if (baselineManager == null)
        {
            baselineManager = WaveBaselineManager.Instance != null
                ? WaveBaselineManager.Instance
                : FindFirstObjectByType<WaveBaselineManager>();
        }

        if (recorder == null)
        {
            recorder = WavePerformanceRecorder.Instance != null
                ? WavePerformanceRecorder.Instance
                : FindFirstObjectByType<WavePerformanceRecorder>();
        }
    }

    private void SubscribeToRecorder()
    {
        ResolveReferences();
        if (recorder == null || recorder == subscribedRecorder)
            return;

        UnsubscribeFromRecorder();
        subscribedRecorder = recorder;
        subscribedRecorder.WaveCompleted += EvaluateCompletedWave;
    }

    private void UnsubscribeFromRecorder()
    {
        if (subscribedRecorder != null)
            subscribedRecorder.WaveCompleted -= EvaluateCompletedWave;
        subscribedRecorder = null;
    }

    public WaveMetricScores GetMetricScores(WavePerformanceSnapshot snapshot)
    {
        ResolveReferences();
        if (baselineManager != null)
            return baselineManager.GetMetricScores(snapshot);

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

    public void EvaluateCompletedWave(WavePerformanceSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        ResolveReferences();
        if (waveDirector == null || baselineManager == null)
        {
            Debug.LogWarning("TempoDirector needs WaveDirector and WaveBaselineManager to score completed waves.", this);
            return;
        }

        lastExpectedRoundDuration = baselineManager.EstimateExpectedRoundDuration(snapshot);
        lastExpectedKillCadence = baselineManager.EstimateExpectedKillCadence(snapshot);
        WaveMetricScores metrics = baselineManager.GetMetricScores(snapshot);

        float weightedScore = 0f;
        float totalWeight = 0f;

        AddMetric(metrics.roundTime, waveDirector.RoundTimeWeight, ref weightedScore, ref totalWeight, metrics.hasRoundTime);
        AddMetric(metrics.killCadence, waveDirector.KillCadenceWeight, ref weightedScore, ref totalWeight, metrics.hasKillCadence);
        AddMetric(metrics.timeToFirstDamage, waveDirector.FirstDamageWeight, ref weightedScore, ref totalWeight, metrics.hasFirstDamage);
        AddMetric(metrics.movementPercentage, waveDirector.MovementWeight, ref weightedScore, ref totalWeight, metrics.hasMovementSamples);
        AddMetric(metrics.damageTaken, waveDirector.DamageTakenWeight, ref weightedScore, ref totalWeight, metrics.hasDamageTaken);
        AddMetric(metrics.damageDealt, waveDirector.DamageDealtWeight, ref weightedScore, ref totalWeight, metrics.hasDamageDealt);
        AddMetric(metrics.points, waveDirector.PointsWeight, ref weightedScore, ref totalWeight, metrics.hasPoints);

        float rawScore = totalWeight > 0f ? weightedScore / totalWeight : 50f;
        rawScore = Mathf.Clamp(rawScore, 0f, 100f);

        tempoScore = waveDirector.UseScoreSmoothing && hasPreviousScore
            ? Mathf.Lerp(rawScore, previousSmoothedScore, waveDirector.PreviousScoreInfluence)
            : rawScore;

        previousSmoothedScore = tempoScore;
        hasPreviousScore = true;
        nextIntermissionSeconds = CalculateIntermission(tempoScore);
        completedWavesEvaluated++;

        // Score against the previous learned baseline, then learn from this wave.
        baselineManager.LearnFromCompletedWave(snapshot);

        Debug.Log(
            $"Tempo Director: Round {snapshot.roundNumber} complete. " +
            $"Duration {metrics.roundTime:F0}, " +
            $"kill cadence {(metrics.hasKillCadence ? metrics.killCadence.ToString("F0") : "n/a")}, " +
            $"first damage {(metrics.hasFirstDamage ? metrics.timeToFirstDamage.ToString("F0") : "n/a")}, " +
            $"damage taken {metrics.damageTaken:F0}, damage dealt {metrics.damageDealt:F0}, " +
            $"points {metrics.points:F0}, overall {tempoScore:F1}/100; " +
            $"next intermission {nextIntermissionSeconds:F1}s; learned waves {baselineManager.CompletedWavesLearned}.",
            this);
    }

    private float CalculateIntermission(float score)
    {
        if (waveDirector == null)
            return 12f;

        if (score < 50f)
        {
            float t = Mathf.InverseLerp(0f, 50f, score);
            return Mathf.Lerp(waveDirector.MaximumIntermission, waveDirector.DefaultIntermissionSeconds, t);
        }

        float highT = Mathf.InverseLerp(50f, 100f, score);
        return Mathf.Lerp(waveDirector.DefaultIntermissionSeconds, waveDirector.MinimumIntermission, highT);
    }

    public void ResetForNewRun()
    {
        ResolveReferences();
        if (baselineManager != null)
            baselineManager.ResetToDefaults();

        tempoScore = 50f;
        nextIntermissionSeconds = waveDirector != null ? waveDirector.DefaultIntermission : 12f;
        lastExpectedRoundDuration = 0f;
        lastExpectedKillCadence = 0f;
        completedWavesEvaluated = 0;
        previousSmoothedScore = 50f;
        hasPreviousScore = false;
    }

    private static void AddMetric(float score, float weight, ref float weightedScore, ref float totalWeight, bool available)
    {
        if (!available || weight <= 0f)
            return;

        weightedScore += Mathf.Clamp(score, 0f, 100f) * weight;
        totalWeight += weight;
    }
}
