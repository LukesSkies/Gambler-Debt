using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WaveSpawner : MonoBehaviour
{
    // Auto-discovered scene dependency; no manual Inspector wiring required.
    private ZombieSpawner zombieSpawner;
    private const bool RandomiseSpawnPoint = true;

    private WaveConfiguration configuration;
    private readonly List<ZombieSpawnPoint> spawnPoints = new List<ZombieSpawnPoint>();

    private int spawnedCount;
    private int walkerQuota;
    private int joggerQuota;
    private int runnerQuota;
    private float nextSpawnTime;
    private bool wasAtActiveCap;
    private bool waveActive;

    public int SpawnedCount => spawnedCount;
    public int RemainingToSpawn => configuration != null
        ? Mathf.Max(0, configuration.totalDemonCount - spawnedCount)
        : 0;

    public int ActiveEnemyCount => zombieSpawner != null
        ? Mathf.Max(0, zombieSpawner.CurrentZombieCount)
        : 0;

    public bool IsWaveCleared
    {
        get
        {
            return waveActive &&
                   configuration != null &&
                   spawnedCount >= configuration.totalDemonCount &&
                   zombieSpawner != null &&
                   zombieSpawner.RoundZombieCount <= 0 &&
                   zombieSpawner.CurrentZombieCount <= 0;
        }
    }

    private void Awake()
    {
        FindZombieSpawner();
    }

    private void Update()
    {
        if (!waveActive || configuration == null || zombieSpawner == null)
            return;

        if (spawnedCount >= configuration.totalDemonCount)
            return;

        int activeCount = Mathf.Max(0, zombieSpawner.CurrentZombieCount);
        int activeCap = Mathf.Max(1, configuration.maximumActiveDemons);

        // Match the old spawner's behaviour: once the active cap has been reached,
        // wait a full configured interval after a slot becomes free before spawning
        // another batch.
        if (activeCount >= activeCap)
        {
            wasAtActiveCap = true;
            return;
        }

        if (wasAtActiveCap)
        {
            wasAtActiveCap = false;
            nextSpawnTime = Time.time + Mathf.Max(0.01f, configuration.spawnInterval);
            return;
        }

        if (Time.time < nextSpawnTime)
            return;

        int availableCapacity = activeCap - activeCount;
        int batchSize = Mathf.Clamp(configuration.spawnBatchLimit, 1, activeCap);
        int batchCount = Mathf.Min(
            batchSize,
            Mathf.Min(availableCapacity, configuration.totalDemonCount - spawnedCount));

        bool spawnedAnything = false;

        for (int i = 0; i < batchCount; i++)
        {
            if (!SpawnOneEnemy())
            {
                // A spawn point can be blocked by an enemy still leaving its
                // spawn area. Retry without consuming the pending enemy quota.
                nextSpawnTime = Time.time + Mathf.Max(0.1f, configuration.spawnRetryInterval);
                return;
            }

            spawnedAnything = true;
        }

        if (spawnedAnything)
            nextSpawnTime = Time.time + Mathf.Max(0.01f, configuration.spawnInterval);
    }

    // Begins executing the exact configuration already passed to Recorder.
    public bool BeginWave(WaveConfiguration waveConfiguration)
    {
        if (waveConfiguration == null)
        {
            Debug.LogError("WaveSpawner.BeginWave received a null configuration.", this);
            return false;
        }

        FindZombieSpawner();
        if (zombieSpawner == null)
        {
            Debug.LogError("WaveSpawner needs the existing ZombieSpawner in the scene.", this);
            return false;
        }

        // Stop the old component's Update loop. Its Awake-created singleton and
        // public counters remain available to existing EnemyHealth instances.
        zombieSpawner.enabled = false;

        configuration = waveConfiguration;
        spawnedCount = 0;
        wasAtActiveCap = false;
        waveActive = true;

        CalculateTypeQuotas(
            configuration.totalDemonCount,
            configuration.walkerPercentage,
            configuration.joggerPercentage,
            configuration.runnerPercentage,
            out walkerQuota,
            out joggerQuota,
            out runnerQuota);

        // Existing EnemyHealth.Death decrements the round counts only when a zombie dies.
        // Each round count therefore represents living enemies plus not-yet-spawned enemies.
        zombieSpawner.CurrentZombieCount = 0;
        zombieSpawner.CurrentWalkingZombieCount = 0;
        zombieSpawner.CurrentJoggingZombieCount = 0;
        zombieSpawner.CurrentRunningZombieCount = 0;

        zombieSpawner.RoundZombieCount = configuration.totalDemonCount;
        zombieSpawner.RoundZombieWalkingCount = walkerQuota;
        zombieSpawner.RoundZombieJoggingCount = joggerQuota;
        zombieSpawner.RoundZombieRunningCount = runnerQuota;
        zombieSpawner.StartZombieSpawer = false;
        zombieSpawner.ZombieSpawnerTimer = 0f;

        RefreshSpawnPoints();
        nextSpawnTime = Time.time + Mathf.Max(0.01f, configuration.spawnInterval);

        Debug.Log(
            $"WaveSpawner executing Round {configuration.roundNumber}: " +
            $"{configuration.totalDemonCount} total, {configuration.maximumActiveDemons} active cap, " +
            $"{configuration.walkerPercentage}% Walkers / {configuration.joggerPercentage}% Joggers / " +
            $"{configuration.runnerPercentage}% Runners.",
            this);

        return true;
    }

    public void TakeOverLegacySpawner()
    {
        FindZombieSpawner();
        if (zombieSpawner != null)
            zombieSpawner.enabled = false;
    }

    public void StopWave()
    {
        waveActive = false;
    }

    private bool SpawnOneEnemy()
    {
        RefreshSpawnPoints();

        List<ZombieSpawnPoint> availablePoints = new List<ZombieSpawnPoint>();
        foreach (ZombieSpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint != null && spawnPoint.isActiveAndEnabled && spawnPoint.CheckIfEnemyCanSpawn())
                availablePoints.Add(spawnPoint);
        }

        if (availablePoints.Count == 0)
            return false;

        int startIndex = RandomiseSpawnPoint
            ? Random.Range(0, availablePoints.Count)
            : 0;

        for (int attempt = 0; attempt < availablePoints.Count; attempt++)
        {
            int index = (startIndex + attempt) % availablePoints.Count;
            ZombieSpawnPoint selectedPoint = availablePoints[index];
            Transform emptySlot = FindFirstEmptySpawnSlot(selectedPoint);

            if (emptySlot == null)
                continue;

            EnemyKind kind = ChooseNextEnemyKind();
            bool canRun = kind != EnemyKind.Walker;
            bool jogging = kind == EnemyKind.Jogger;

            // Uses the original spawning method, preserving prefab instantiation,
            // NavMesh speed assignment, EnemyAI flags and EnemyHealth type setup.
            selectedPoint.SpawnZombie(canRun, jogging);

            if (emptySlot.childCount == 0)
                continue;

            GameObject enemy = emptySlot.GetChild(0).gameObject;
            RegisterSpawnedEnemy(enemy, kind);
            return true;
        }

        return false;
    }

    private void RegisterSpawnedEnemy(GameObject enemy, EnemyKind kind)
    {
        spawnedCount++;
        zombieSpawner.CurrentZombieCount++;

        switch (kind)
        {
            case EnemyKind.Walker:
                zombieSpawner.CurrentWalkingZombieCount++;
                break;
            case EnemyKind.Jogger:
                zombieSpawner.CurrentJoggingZombieCount++;
                break;
            case EnemyKind.Runner:
                zombieSpawner.CurrentRunningZombieCount++;
                break;
        }

        WaveEnemyRecorderBridge recorderBridge = enemy.GetComponent<WaveEnemyRecorderBridge>();
        if (recorderBridge == null)
            recorderBridge = enemy.AddComponent<WaveEnemyRecorderBridge>();

        recorderBridge.Configure(configuration.baseDemonHealth, configuration.baseDemonSwipeDamage, 0);
    }

    private EnemyKind ChooseNextEnemyKind()
    {
        int pendingWalkers = Mathf.Max(
            0,
            zombieSpawner.RoundZombieWalkingCount - zombieSpawner.CurrentWalkingZombieCount);
        int pendingJoggers = Mathf.Max(
            0,
            zombieSpawner.RoundZombieJoggingCount - zombieSpawner.CurrentJoggingZombieCount);
        int pendingRunners = Mathf.Max(
            0,
            zombieSpawner.RoundZombieRunningCount - zombieSpawner.CurrentRunningZombieCount);

        int pendingTotal = pendingWalkers + pendingJoggers + pendingRunners;
        if (pendingTotal <= 0)
            return EnemyKind.Walker;

        int roll = Random.Range(0, pendingTotal);
        if (roll < pendingWalkers)
            return EnemyKind.Walker;

        roll -= pendingWalkers;
        if (roll < pendingJoggers)
            return EnemyKind.Jogger;

        return EnemyKind.Runner;
    }

    private void RefreshSpawnPoints()
    {
        FindZombieSpawner();
        spawnPoints.Clear();

        if (zombieSpawner != null)
        {
            foreach (GameObject spawnerObject in zombieSpawner.ActiveSpawners)
            {
                if (spawnerObject == null || !spawnerObject.activeInHierarchy)
                    continue;

                ZombieSpawnPoint spawnPoint = spawnerObject.GetComponent<ZombieSpawnPoint>();
                if (spawnPoint != null && !spawnPoints.Contains(spawnPoint))
                    spawnPoints.Add(spawnPoint);
            }
        }

        if (spawnPoints.Count == 0)
        {
            ZombieSpawnPoint[] found = FindObjectsByType<ZombieSpawnPoint>(FindObjectsSortMode.None);
            foreach (ZombieSpawnPoint spawnPoint in found)
            {
                if (spawnPoint != null && spawnPoint.isActiveAndEnabled && !spawnPoints.Contains(spawnPoint))
                    spawnPoints.Add(spawnPoint);
            }
        }
    }

    private static Transform FindFirstEmptySpawnSlot(ZombieSpawnPoint spawnPoint)
    {
        Transform spawnPointParent = spawnPoint.transform.Find("SpawnPoints");
        if (spawnPointParent == null)
            return null;

        foreach (Transform slot in spawnPointParent)
        {
            if (slot.childCount == 0)
                return slot;
        }

        return null;
    }

    private void FindZombieSpawner()
    {
        if (zombieSpawner == null)
            zombieSpawner = FindFirstObjectByType<ZombieSpawner>();
    }

    private static void CalculateTypeQuotas(
        int total,
        int walkerPercent,
        int joggerPercent,
        int runnerPercent,
        out int walkers,
        out int joggers,
        out int runners)
    {
        float walkerExact = total * Mathf.Clamp(walkerPercent, 0, 100) / 100f;
        float joggerExact = total * Mathf.Clamp(joggerPercent, 0, 100) / 100f;
        float runnerExact = total * Mathf.Clamp(runnerPercent, 0, 100) / 100f;

        walkers = Mathf.FloorToInt(walkerExact);
        joggers = Mathf.FloorToInt(joggerExact);
        runners = Mathf.FloorToInt(runnerExact);

        int unassigned = Mathf.Max(0, total - walkers - joggers - runners);
        float walkerRemainder = walkerExact - walkers;
        float joggerRemainder = joggerExact - joggers;
        float runnerRemainder = runnerExact - runners;

        while (unassigned > 0)
        {
            if (walkerRemainder >= joggerRemainder && walkerRemainder >= runnerRemainder)
            {
                walkers++;
                walkerRemainder = -1f;
            }
            else if (joggerRemainder >= runnerRemainder)
            {
                joggers++;
                joggerRemainder = -1f;
            }
            else
            {
                runners++;
                runnerRemainder = -1f;
            }

            unassigned--;
        }

        // Defensive correction if malformed percentages sum to more than 100.
        while (walkers + joggers + runners > total)
        {
            if (walkers >= joggers && walkers >= runners && walkers > 0)
                walkers--;
            else if (joggers >= runners && joggers > 0)
                joggers--;
            else if (runners > 0)
                runners--;
            else
                break;
        }
    }

    private enum EnemyKind
    {
        Walker,
        Jogger,
        Runner
    }
}
