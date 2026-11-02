using UnityEngine;

/// <summary>
/// Temporary keyboard tester for the first wave/tempo implementation.
/// Remove it once real gameplay events are connected.
///
/// F5 = prepare and start recording the next test wave.
/// F6 = finish the recording and calculate the Tempo Score/intermission.
/// 1  = record simulated damage dealt.
/// 2  = record one kill event (press at different times to test average kill time).
/// 3  = record simulated damage taken.
/// 4  = record simulated points earned.
/// 5  = toggle simulated movement.
/// 
/// </summary>
public sealed class WaveSystemPlaceholderTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WavePerformanceRecorder recorder;
    [SerializeField] private WaveDirector waveDirector;

    [Header("Test Settings")]
    [SerializeField, Range(1, 4)] private int testPlayerCount = 1;

    [Header("Simulated Values")]
    [SerializeField, Min(0f)] private float testDamageDealt = 150f;
    [SerializeField, Min(0f)] private float testDamageTaken = 50f;
    [SerializeField, Min(0)] private int testPoints = 100;

    private bool simulateMovement;

    private void Start()
    {
        FindReferences();

        if (recorder == null)
            Debug.LogError("Placeholder Tester: WavePerformanceRecorder not found.");
        if (waveDirector == null)
            Debug.LogWarning("Placeholder Tester: WaveDirector not found. Test waves cannot capture their actual configuration.");
    }

    private void Update()
    {
        if (recorder == null)
            return;

        recorder.RecordMovement(simulateMovement, Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.F5))
            StartNextTestWave();

        if (Input.GetKeyDown(KeyCode.F6))
            recorder.EndWave();

        if (Input.GetKeyDown(KeyCode.Alpha1))
            recorder.RecordDamageDealt(testDamageDealt);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            recorder.RecordKill();

        if (Input.GetKeyDown(KeyCode.Alpha3))
            recorder.RecordDamageTaken(testDamageTaken);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            recorder.RecordPointsEarned(testPoints);

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            simulateMovement = !simulateMovement;
            Debug.Log($"Simulated movement: {(simulateMovement ? "ON" : "OFF")}");
        }
    }

    private void StartNextTestWave()
    {
        int nextRound = Mathf.Max(1, recorder.CurrentRound + 1);
        testPlayerCount = Mathf.Clamp(testPlayerCount, 1, 4);

        // Prepare first so the Recorder captures the exact workload planned by the Director.
        WaveConfiguration configuration = waveDirector != null
            ? waveDirector.PreviewWave(nextRound, testPlayerCount)
            : null;

        if (configuration != null)
            recorder.BeginWave(configuration);
        else
            recorder.BeginWave(nextRound, testPlayerCount);

        WaveDirectorDebugUI[] waveUIs = FindObjectsByType<WaveDirectorDebugUI>(FindObjectsSortMode.None);
        foreach (WaveDirectorDebugUI waveUI in waveUIs)
            waveUI.RefreshDisplay();

        TempoDebugUI[] tempoUIs = FindObjectsByType<TempoDebugUI>(FindObjectsSortMode.None);
        foreach (TempoDebugUI tempoUI in tempoUIs)
            tempoUI.RefreshDisplay();

        Debug.Log($"TEST: Round {nextRound} started. Recorder values reset.");
    }

    private void FindReferences()
    {
        if (recorder == null)
        {
            recorder = WavePerformanceRecorder.Instance != null
                ? WavePerformanceRecorder.Instance
                : FindFirstObjectByType<WavePerformanceRecorder>();
        }

        if (waveDirector == null)
        {
            waveDirector = WaveDirector.Instance != null
                ? WaveDirector.Instance
                : FindFirstObjectByType<WaveDirector>();
        }
    }
}
