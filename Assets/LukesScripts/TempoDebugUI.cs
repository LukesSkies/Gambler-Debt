using TMPro;
using UnityEngine;

public sealed class TempoDebugUI : MonoBehaviour
{
    private WavePerformanceRecorder recorder;
    private TempoDirector tempoDirector;

    [Header("Round")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text roundTimerText;

    [Header("Kill Cadence")]
    [SerializeField] private TMP_Text averageKillTimeText;

    [Header("First Damage and Movement")]
    [SerializeField] private TMP_Text firstDamageTimerText;
    [SerializeField] private TMP_Text movementTimerText;
    [SerializeField] private TMP_Text movementPercentageText;

    [Header("Totals")]
    [SerializeField] private TMP_Text damageTakenText;
    [SerializeField] private TMP_Text damageDealtText;
    [SerializeField] private TMP_Text pointsText;

    [Header("Tempo Director")]
    [SerializeField] private TMP_Text tempoScoreText;
    [SerializeField] private TMP_Text nextIntermissionText;

    [Header("Performance Colours")]
    [SerializeField] private Color greatColour = new Color(0.05f, 0.85f, 0.15f);
    [SerializeField] private Color middleColour = new Color(1f, 0.82f, 0.05f);
    [SerializeField] private Color belowAverageColour = new Color(1f, 0.12f, 0.12f);
    [SerializeField] private Color reallyBadColour = new Color(1f, 0.62f, 0.05f);

    private void Start()
    {
        FindReferences();
        RefreshDisplay();
    }

    private void Update()
    {
        // A debug UI benefits from live timer updates. The Tempo Score itself is
        // calculated only when a completed snapshot reaches TempoDirector.
        FindReferences();
        RefreshDisplay();
    }

    // Copies displayed values from Recorder and colours each metric using the
    // corresponding workload-normalized score provided by TempoDirector.
    public void RefreshDisplay()
    {
        FindReferences();

        WavePerformanceSnapshot snapshot = recorder != null ? recorder.GetCurrentSnapshot() : null;
        WaveMetricScores scores = tempoDirector != null
            ? tempoDirector.GetMetricScores(snapshot)
            : CreateNeutralScores();

        if (recorder != null)
        {
            if (roundText != null)
                roundText.text = recorder.CurrentRound.ToString();

            if (roundTimerText != null)
            {
                roundTimerText.text = FormatTime(recorder.ElapsedTime);
                roundTimerText.color = GetScoreColour(scores.roundTime);
            }

            if (averageKillTimeText != null)
            {
                averageKillTimeText.text = recorder.HasKillCadence
                    ? FormatTime(recorder.AverageTimeBetweenKills)
                    : "--";
                averageKillTimeText.color = recorder.HasKillCadence
                    ? GetScoreColour(scores.killCadence)
                    : middleColour;
            }

            if (firstDamageTimerText != null)
            {
                // Before first damage, the Recorder's elapsed time is used by
                // TempoDirector to produce a live score for this timer.
                firstDamageTimerText.text = recorder.HasFirstDamage
                    ? FormatTime(recorder.FirstDamageTime)
                    : (recorder.HasStarted ? FormatTime(recorder.ElapsedTime) : "--");
                firstDamageTimerText.color = GetScoreColour(scores.timeToFirstDamage);
            }

            if (movementTimerText != null)
            {
                movementTimerText.text = FormatTime(recorder.MovementTime);
                movementTimerText.color = recorder.HasMovementSamples
                    ? GetScoreColour(scores.movement)
                    : middleColour;
            }

            if (movementPercentageText != null)
            {
                movementPercentageText.text = $"{recorder.MovementPercentage:F0}%";
                movementPercentageText.color = recorder.HasMovementSamples
                    ? GetScoreColour(scores.movementPercentage)
                    : middleColour;
            }

            if (damageTakenText != null)
            {
                damageTakenText.text = recorder.DamageTaken.ToString("F0");
                damageTakenText.color = GetScoreColour(scores.damageTaken);
            }

            if (damageDealtText != null)
            {
                damageDealtText.text = recorder.DamageDealt.ToString("F0");
                damageDealtText.color = GetScoreColour(scores.damageDealt);
            }

            if (pointsText != null)
            {
                pointsText.text = recorder.PointsEarned.ToString();
                pointsText.color = GetScoreColour(scores.points);
            }
        }
        else
        {
            // Even if the Recorder is not in the scene yet, keep text colours
            // predictable rather than leaving stale colours from a previous scene.
            SetMetricTextColours(middleColour);
        }

        if (tempoDirector != null)
        {
            if (tempoScoreText != null)
            {
                tempoScoreText.text = tempoDirector.TempoScore.ToString("F1");
                tempoScoreText.color = GetScoreColour(tempoDirector.TempoScore);
            }

            if (nextIntermissionText != null)
                nextIntermissionText.text = tempoDirector.NextIntermissionSeconds.ToString("F1");
        }
    }

    private void FindReferences()
    {
        if (recorder == null)
        {
            recorder = WavePerformanceRecorder.Instance != null
                ? WavePerformanceRecorder.Instance
                : FindFirstObjectByType<WavePerformanceRecorder>();
        }

        if (tempoDirector == null)
        {
            tempoDirector = TempoDirector.Instance != null
                ? TempoDirector.Instance
                : FindFirstObjectByType<TempoDirector>();
        }
    }

    private void SetMetricTextColours(Color colour)
    {
        if (roundTimerText != null) roundTimerText.color = colour;
        if (averageKillTimeText != null) averageKillTimeText.color = colour;
        if (firstDamageTimerText != null) firstDamageTimerText.color = colour;
        if (movementTimerText != null) movementTimerText.color = colour;
        if (movementPercentageText != null) movementPercentageText.color = colour;
        if (damageTakenText != null) damageTakenText.color = colour;
        if (damageDealtText != null) damageDealtText.color = colour;
        if (pointsText != null) pointsText.color = colour;
    }

    private WaveMetricScores CreateNeutralScores()
    {
        // Without TempoDirector there is no comparison baseline. Neutral scores
        // keep fields readable until the manager is available.
        return new WaveMetricScores
        {
            roundTime = 60f,
            killCadence = 60f,
            timeToFirstDamage = 60f,
            movement = 60f,
            movementPercentage = 60f,
            damageTaken = 60f,
            damageDealt = 60f,
            points = 60f
        };
    }

    // Maps score bands to the requested colours. Very poor is intentionally
    private Color GetScoreColour(float score)
    {
        if (score >= 75f) return greatColour;
        if (score >= 50f) return middleColour;
        if (score >= 20f) return belowAverageColour;
        return reallyBadColour;
    }

    // Formats elapsed seconds as MM:SS.s, e.g. 65.3 becomes 01:05.3
    private static string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(seconds / 60f);
        float remainingSeconds = seconds - minutes * 60f;
        return $"{minutes:00}:{remainingSeconds:00.0}";
    }
}
