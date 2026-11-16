using TMPro;
using UnityEngine;

public sealed class TempoDebugUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WavePerformanceRecorder recorder;

    [SerializeField] private TempoDirector tempoDirector;

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

    [Tooltip("Yellow/orange used for metric scores below 20, representing really poor performance.")]
    [SerializeField] private Color reallyBadColour = new Color(1f, 0.62f, 0.05f);

    // Looks up the Recorder and TempoDirector once when the UI starts, then fills
    // the text fields for the first time. The Inspector references are preferred.
    private void Start()
    {
        FindReferences();
        RefreshDisplay();
    }

    // Refreshes the debug fields every frame so the round timer, movement timer,
    // first-hit timer and their colours can change while a wave is active.
    //
    // This per-frame refresh is acceptable for a debug panel. If this becomes a
    // production HUD, consider having Recorder/TempoDirector raise value-changed
    // events and refresh only when data changes. Do not move recording logic into
    // this UI; live gameplay scripts must still report events to the Recorder.
    private void Update()
    {
        FindReferences();
        RefreshDisplay();
    }

    // Reads the current Recorder snapshot, gets the metric scores from TempoDirector,
    // writes the displayed values, and colours each metric text field.
    public void RefreshDisplay()
    {
        FindReferences();

        WavePerformanceSnapshot snapshot = recorder != null
            ? recorder.GetCurrentSnapshot()
            : null;

        WaveMetricScores scores = tempoDirector != null
            ? tempoDirector.GetMetricScores(snapshot)
            : CreateNeutralScores();

        if (recorder != null)
        {
            if (roundText != null)
                roundText.text = recorder.CurrentRound.ToString();

            // ROUND TIME: the displayed value is the Recorder's elapsed wave time.
            // A shorter time relative to the expected workload gets a better score.
            if (roundTimerText != null)
            {
                roundTimerText.text = FormatTime(recorder.ElapsedTime);
                roundTimerText.color = GetScoreColour(scores.roundTime);
            }

            // KILL CADENCE: this is per-player kill interval data from Recorder.
            // It is not the interval between alternating team kills and not time to
            // first kill. It remains neutral until a valid interval has been recorded.
            if (averageKillTimeText != null)
            {
                averageKillTimeText.text = recorder.HasKillCadence
                    ? FormatTime(recorder.AverageTimeBetweenKills)
                    : "--";

                averageKillTimeText.color = recorder.HasKillCadence
                    ? GetScoreColour(scores.killCadence)
                    : middleColour;
            }

            // TIME TO FIRST DAMAGE: once the first successful player hit occurs,
            // display its recorded time. Before that, display the live elapsed time
            // so the team can see how long it has gone without hitting an enemy.
            if (firstDamageTimerText != null)
            {
                firstDamageTimerText.text = recorder.HasFirstDamage
                    ? FormatTime(recorder.FirstDamageTime)
                    : (recorder.HasStarted ? FormatTime(recorder.ElapsedTime) : "--");

                firstDamageTimerText.color = GetScoreColour(scores.timeToFirstDamage);
            }

            // MOVEMENT TIME: Recorder adds deltaTime only on samples where the
            // player was actually moving. The display is averaged across players.
            // The movement percentage below determines the colour: higher movement
            // is better, and 100% should always be green.
            if (movementTimerText != null)
            {
                movementTimerText.text = FormatTime(recorder.MovementTime);
                movementTimerText.color = recorder.HasMovementSamples
                    ? GetScoreColour(scores.movement)
                    : middleColour;
            }

            // MOVEMENT PERCENTAGE: the denominator includes both moving and still
            // time for the entire round. It uses the same score as Movement Time
            // because both fields describe the same underlying movement behaviour.
            if (movementPercentageText != null)
            {
                movementPercentageText.text = $"{recorder.MovementPercentage:F0}%";
                movementPercentageText.color = recorder.HasMovementSamples
                    ? GetScoreColour(scores.movementPercentage)
                    : middleColour;
            }

            // DAMAGE TAKEN: show the accumulated total. A wave with no damage is
            // explicitly green. Otherwise WaveBaselineManager compares the total
            // with the full-wave expected amount; the threshold must not shrink just
            // because elapsed time changes.
            if (damageTakenText != null)
            {
                damageTakenText.text = recorder.DamageTaken.ToString("F0");

                if (recorder.HasStarted && recorder.DamageTaken <= 0f)
                {
                    damageTakenText.color = greatColour;
                }
                else
                {
                    damageTakenText.color = scores.hasDamageTaken
                        ? GetScoreColour(scores.damageTaken)
                        : middleColour;
                }
            }

            // DAMAGE DEALT: display the total health removed from enemies. The
            // baseline compares this with total configured enemy health. At the
            // end of a fully cleared wave it will normally reach 100; round time
            // and kill cadence provide the speed comparison.
            if (damageDealtText != null)
            {
                damageDealtText.text = recorder.DamageDealt.ToString("F0");
                damageDealtText.color = scores.hasDamageDealt
                    ? GetScoreColour(scores.damageDealt)
                    : middleColour;
            }

            // POINTS: display the total actually paid by the points/economy script.
            // The colour compares the total with the estimated maximum opportunity,
            // not points earned per second, so a longer round does not reduce it.
            if (pointsText != null)
            {
                pointsText.text = recorder.PointsEarned.ToString();
                pointsText.color = scores.hasPoints
                    ? GetScoreColour(scores.points)
                    : middleColour;
            }
        }
        else
        {
            // Clear stale colours if the UI is active in a scene where the Recorder
            // has not been created/found yet. The values themselves are left alone.
            SetMetricColours(middleColour);
        }

        // These two values are updated by TempoDirector after a wave is evaluated.
        // During an active wave they continue to show the most recently calculated
        // result (or the initial defaults before the first wave has completed).
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

    // Finds the Recorder and TempoDirector if the Inspector fields were not set.
    // The singleton is preferred, with a scene lookup as a fallback.
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

    // Applies one fallback colour to every metric field. This is used only when
    // the Recorder is unavailable, so the UI does not retain old colours from
    // a previous scene or previous manager instance.
    private void SetMetricColours(Color colour)
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

    // Converts the numeric metric score into the project's requested colour band:
    // 75+ green, 50-74 yellow, 20-49 orange, and below 20 red.
    private Color GetScoreColour(float score)
    {
        if (score >= 75f) return greatColour;
        if (score >= 50f) return middleColour;
        if (score >= 20f) return belowAverageColour;
        return reallyBadColour;
    }

    // Supplies neutral placeholder scores if TempoDirector is not present.
    private static WaveMetricScores CreateNeutralScores()
    {
        return new WaveMetricScores
        {
            roundTime = 60f,
            killCadence = 60f,
            timeToFirstDamage = 60f,
            movement = 60f,
            movementPercentage = 60f,
            damageTaken = 60f,
            damageDealt = 60f,
            points = 60f,
            hasRoundTime = true,
            hasKillCadence = true,
            hasFirstDamage = true,
            hasMovementSamples = true,
            hasDamageTaken = true,
            hasDamageDealt = true,
            hasPoints = true
        };
    }

    // Formats a duration as MM:SS.s for display only.
    private static string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(seconds / 60f);
        float remainingSeconds = seconds - minutes * 60f;
        return $"{minutes:00}:{remainingSeconds:00.0}";
    }
}
