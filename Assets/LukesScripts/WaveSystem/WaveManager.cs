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

    [Header("Optional HUD")]
    [SerializeField] private TMP_Text roundNumberText;

    [Header("Start")]
    [SerializeField] private bool startFirstWaveAutomatically = true;

    private WaveDirector waveDirector;
    private WaveSpawner waveSpawner;
    private WavePerformanceRecorder recorder;
    private TempoDirector tempoDirector;
    private GameManager gameManager;
    private RoundSystem roundSystem;

    private int currentRound;
    private int currentPlayerCount = 1;
    private float lastIntermissionUsed;
    private bool waveActive;
    private bool runInitialized;
    private WaveState state = WaveState.WaitingToStart;
    private Coroutine intermissionCoroutine;
    private Coroutine completionCoroutine;

    public int CurrentRound => currentRound;
    public int CurrentPlayerCount => currentPlayerCount;
    public bool WaveActive => waveActive;
    public float LastIntermissionUsed => lastIntermissionUsed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple WaveManager instances detected. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FindReferences();
    }

    private IEnumerator Start()
    {
        // Allow the existing GameManager, RoundSystem and spawn points to initialise.
        yield return null;
        yield return new WaitForEndOfFrame();

        FindReferences();
        if (waveDirector == null || waveSpawner == null || recorder == null || gameManager == null)
        {
            Debug.LogError("WaveManager needs WaveDirector, WaveSpawner, WavePerformanceRecorder and GameManager in the scene.", this);
            state = WaveState.Stopped;
            yield break;
        }

        if (tempoDirector == null)
            tempoDirector = FindFirstObjectByType<TempoDirector>();

        waveSpawner.TakeOverLegacySpawner();

        if (startFirstWaveAutomatically)
            StartWave(waveDirector.FirstRoundToStart);
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

        FindReferences();
        if (waveDirector != null)
            StartWave(waveDirector.FirstRoundToStart);
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
            Debug.LogError("WaveManager is missing a required runtime dependency; wave start cancelled.", this);
            state = WaveState.Stopped;
            return;
        }

        InitializeRunIfNeeded();
        currentPlayerCount = ResolvePlayerCount();
        currentRound = Mathf.Max(1, roundNumber);
        gameManager.Round = currentRound;

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

        if (!waveSpawner.BeginWave(configuration))
        {
            state = WaveState.Stopped;
            return;
        }

        // Recorder and Spawner receive the same configuration before the first spawn.
        recorder.BeginWave(configuration);

        if (roundNumberText != null)
            roundNumberText.text = currentRound.ToString();

        waveActive = true;
        state = WaveState.Active;
        Debug.Log($"WaveManager started Round {currentRound}: {configuration.totalDemonCount} demons, {currentPlayerCount} player(s).", this);
    }

    private IEnumerator CompleteAfterDestroyCallbacks()
    {
        // Let EnemyHealth.OnDestroy and WaveEnemyRecorderBridge finish recording the final hit/kill.
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
        recorder.EndWave();

        float intermission = tempoDirector != null
            ? tempoDirector.NextIntermissionSeconds
            : (waveDirector != null ? waveDirector.DefaultIntermission : 12f);

        lastIntermissionUsed = Mathf.Max(0f, intermission);
        intermissionCoroutine = StartCoroutine(BeginNextWaveAfterIntermission(lastIntermissionUsed));
        Debug.Log($"WaveManager cleared Round {currentRound}. Next wave in {lastIntermissionUsed:F1} seconds.", this);
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
        return waveDirector != null ? waveDirector.DefaultPlayerCount : 1;
    }

    private void InitializeRunIfNeeded()
    {
        if (runInitialized)
            return;

        if (tempoDirector != null)
            tempoDirector.ResetForNewRun();
        runInitialized = true;
    }

    private void PrepareGameManagerHealthForRound(WaveConfiguration configuration)
    {
        if (configuration == null || configuration.roundNumber <= 9)
            return;

        // Keep the existing GameManager's Round 10+ 1.1x health progression aligned with the configured value.
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
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (roundSystem == null) roundSystem = FindFirstObjectByType<RoundSystem>();
        if (waveDirector == null) waveDirector = WaveDirector.Instance != null ? WaveDirector.Instance : FindFirstObjectByType<WaveDirector>();
        if (waveSpawner == null) waveSpawner = FindFirstObjectByType<WaveSpawner>();
        if (recorder == null) recorder = WavePerformanceRecorder.Instance != null ? WavePerformanceRecorder.Instance : FindFirstObjectByType<WavePerformanceRecorder>();
        if (tempoDirector == null) tempoDirector = TempoDirector.Instance != null ? TempoDirector.Instance : FindFirstObjectByType<TempoDirector>();
    }
}
