using System.Collections;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    private enum WaveState
    {
        WaitingToStart,
        Active,
        Intermission,
        Stopped
    }

    [Header("Manager references")]
    [SerializeField] private WaveDirector waveDirector;
    [SerializeField] private WaveSpawner waveSpawner;
    [SerializeField] private WavePerformanceRecorder recorder;
    [SerializeField] private TempoDirector tempoDirector;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RoundSystem roundSystem;

    [Header("Start settings")]
    [SerializeField] private bool startFirstWaveAutomatically = true;
    [SerializeField, Min(1)] private int startingRound = 1;
    [SerializeField, Range(1, 4)] private int fallbackPlayerCount = 1;
    [SerializeField, Min(0f)] private float fallbackIntermissionSeconds = 12f;
    [SerializeField] private bool resetTempoWhenStartingANewRun = true;

    [Header("Runtime debug")]
    [SerializeField] private int currentRound;
    [SerializeField] private int currentPlayerCount = 1;
    [SerializeField] private float lastIntermissionUsed;
    [SerializeField] private bool waveActive;

    private WaveState state = WaveState.WaitingToStart;
    private Coroutine intermissionCoroutine;
    private Coroutine completionCoroutine;
    private bool runInitialized;

    public int CurrentRound => currentRound;
    public int CurrentPlayerCount => currentPlayerCount;
    public bool WaveActive => waveActive;
    public float LastIntermissionUsed => lastIntermissionUsed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple WaveManager instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FindReferences();
    }

    private IEnumerator Start()
    {
        // Let existing GameManager, RoundSystem and spawn-point Start methods
        // finish before the first configured wave is prepared.
        yield return null;

        FindReferences();
        AttachPlayerBridges();

        if (gameManager == null || waveDirector == null || waveSpawner == null || recorder == null)
        {
            Debug.LogError(
                "WaveManager cannot start. Required references are GameManager, WaveDirector, WaveSpawner and WavePerformanceRecorder.",
                this);
            state = WaveState.Stopped;
            yield break;
        }

        if (tempoDirector == null)
            tempoDirector = FindFirstObjectByType<TempoDirector>();

        // Take ownership even when the first wave is started by a menu later.
        // This prevents the old ZombieSpawner Update loop from auto-advancing.
        waveSpawner.TakeOverLegacySpawner();

        if (startFirstWaveAutomatically)
            StartWave(Mathf.Max(1, startingRound));
        else
            state = WaveState.WaitingToStart;
    }

    private void Update()
    {
        if (state == WaveState.Active && waveActive && waveSpawner != null &&
            waveSpawner.IsWaveCleared && completionCoroutine == null)
        {
            completionCoroutine = StartCoroutine(CompleteAfterDestroyCallbacks());
        }
    }

    private void OnDestroy()
    {
        if (intermissionCoroutine != null)
            StopCoroutine(intermissionCoroutine);

        if (completionCoroutine != null)
            StopCoroutine(completionCoroutine);

        if (Instance == this)
            Instance = null;
    }

    public void StartFirstWave()
    {
        if (state == WaveState.Active || state == WaveState.Intermission)
            return;

        StartWave(Mathf.Max(1, startingRound));
    }

    public void StopWaveProgression()
    {
        if (intermissionCoroutine != null)
        {
            StopCoroutine(intermissionCoroutine);
            intermissionCoroutine = null;
        }

        if (completionCoroutine != null)
        {
            StopCoroutine(completionCoroutine);
            completionCoroutine = null;
        }

        if (waveSpawner != null)
            waveSpawner.StopWave();

        waveActive = false;
        state = WaveState.Stopped;
    }

    private void StartWave(int roundNumber)
    {
        FindReferences();

        if (waveDirector == null || waveSpawner == null || recorder == null || gameManager == null)
        {
            Debug.LogError("WaveManager is missing a required reference and cannot start a wave.", this);
            state = WaveState.Stopped;
            return;
        }

        InitializeRunIfNeeded();

        currentPlayerCount = ResolvePlayerCount();
        currentRound = Mathf.Max(1, roundNumber);

        // Existing EnemyHealth.Awake/Start read GameManager.Round and
        // GameManager.GetZombieHealth to initialise each spawned enemy.
        gameManager.Round = currentRound;

        // WaveDirector creates the real configuration using the latest Tempo Score.
        WaveConfiguration configuration = waveDirector.PreviewWave(currentRound, currentPlayerCount);
        if (configuration == null)
        {
            Debug.LogError("WaveDirector returned a null WaveConfiguration.", this);
            state = WaveState.Stopped;
            return;
        }

        PrepareGameManagerHealthForRound(configuration);
        AttachPlayerBridges();

        if (roundSystem != null)
        {
            roundSystem.ZombieCount = configuration.totalDemonCount;
            roundSystem.ZombieSpawnRate = configuration.spawnInterval;
        }

        // Configure the spawner first. It only schedules its first spawn in the
        // future, so the Recorder can begin before an enemy is instantiated.
        if (!waveSpawner.BeginWave(configuration))
        {
            state = WaveState.Stopped;
            return;
        }

        // Both systems receive the exact same configuration object.
        recorder.BeginWave(configuration);

        waveActive = true;
        state = WaveState.Active;
    }

    private IEnumerator CompleteAfterDestroyCallbacks()
    {
        // EnemyHealth decrements the ZombieSpawner counters before Destroy(gameObject).
        // Wait until the end of the frame so WaveEnemyRecorderBridge.OnDestroy can
        // record the lethal hit and kill before the Recorder freezes its snapshot.
        yield return new WaitForEndOfFrame();
        yield return null;
        completionCoroutine = null;

        if (state == WaveState.Active && waveActive && waveSpawner != null && waveSpawner.IsWaveCleared)
            CompleteCurrentWave();
    }

    private void CompleteCurrentWave()
    {
        if (!waveActive || state != WaveState.Active)
            return;

        waveActive = false;
        state = WaveState.Intermission;
        waveSpawner.StopWave();

        // TempoDirector receives WaveCompleted synchronously from EndWave and
        // calculates the next score/intermission before this call returns.
        recorder.EndWave();

        float intermission = tempoDirector != null
            ? tempoDirector.NextIntermissionSeconds
            : fallbackIntermissionSeconds;

        lastIntermissionUsed = Mathf.Max(0f, intermission);
        intermissionCoroutine = StartCoroutine(BeginNextWaveAfterIntermission(lastIntermissionUsed));

        Debug.Log(
            $"WaveManager cleared Round {currentRound}. Next wave in {lastIntermissionUsed:F1} seconds.",
            this);
    }

    private IEnumerator BeginNextWaveAfterIntermission(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        intermissionCoroutine = null;

        if (state != WaveState.Intermission)
            yield break;

        StartWave(currentRound + 1);
    }

    private int ResolvePlayerCount()
    {
        if (gameManager != null && gameManager.PlayerCount >= 1)
            return Mathf.Clamp(gameManager.PlayerCount, 1, 4);

        return Mathf.Clamp(fallbackPlayerCount, 1, 4);
    }

    private void InitializeRunIfNeeded()
    {
        if (runInitialized)
            return;

        if (resetTempoWhenStartingANewRun && tempoDirector != null)
            tempoDirector.ResetForNewRun();

        runInitialized = true;
    }

    private void PrepareGameManagerHealthForRound(WaveConfiguration configuration)
    {
        if (configuration == null || configuration.roundNumber <= 9)
            return;

        // Existing GameManager.GetZombieHealth(round) multiplies its stored
        // previous-round health by 1.1 for Round 10 onward. Store the preceding
        // value so its existing health initialisation produces this configured HP.
        gameManager.ZombieHealth = Mathf.RoundToInt(configuration.baseDemonHealth / 1.1f);
    }

    private void AttachPlayerBridges()
    {
        PlayerHP[] players = FindObjectsByType<PlayerHP>(FindObjectsSortMode.None);
        foreach (PlayerHP player in players)
        {
            if (player == null)
                continue;

            WavePlayerRecorderBridge bridge = player.GetComponent<WavePlayerRecorderBridge>();
            if (bridge == null)
                bridge = player.gameObject.AddComponent<WavePlayerRecorderBridge>();

            bridge.Initialize();
        }
    }

    private void FindReferences()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (roundSystem == null)
            roundSystem = FindFirstObjectByType<RoundSystem>();

        if (waveDirector == null)
            waveDirector = FindFirstObjectByType<WaveDirector>();

        if (waveSpawner == null)
            waveSpawner = FindFirstObjectByType<WaveSpawner>();

        if (recorder == null)
            recorder = FindFirstObjectByType<WavePerformanceRecorder>();

        if (tempoDirector == null)
            tempoDirector = FindFirstObjectByType<TempoDirector>();
    }
}
