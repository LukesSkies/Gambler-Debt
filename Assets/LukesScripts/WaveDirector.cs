using System;
using UnityEngine;

// How the previous team's performance changes next-wave pressure.
public enum WaveAdaptationProfile
{
    Sweeper,
    Baseline,
    BreathingRoom
}

// Complete plan for one wave. The WaveDirector builds this object; a separate
// WaveSpawner must execute it using the map's authored spawn points.
[Serializable]
public class WaveConfiguration
{
    [Header("Wave")]
    public int roundNumber;
    public int playerCount;
    public int totalDemonCount;
    public int maximumActiveDemons;

    [Header("Population Scaling")]
    public int baselineTotalDemonCount;
    public int populationGrowthBonusDemons;
    public float populationGrowthMultiplier = 1f;

    [Header("Spawning")]
    public float spawnInterval;
    public int spawnBatchLimit;
    public float spawnRetryInterval;

    [Header("Demon Health and Attack Damage")]
    public float baseDemonHealth;
    public float baseDemonSwipeDamage;

    [Header("Demon Composition")]
    [Range(0, 100)] public int walkerPercentage;
    [Range(0, 100)] public int joggerPercentage;
    [Range(0, 100)] public int runnerPercentage;

    [Header("Adaptation (debug only)")]
    public WaveAdaptationProfile adaptationProfile;
    [Range(0f, 100f)] public float performanceScore;
}

/// Prepares the next wave configuration from a round baseline, player count and
/// previous Tempo Score. It never spawns demons, runs their AI, or starts a wave.
///
/// ROUND BASELINES (editable in Inspector)
/// - Round 1 solo: 6 demons, 150 health, 50 damage per swipe, 2-second spawn interval.
/// - Total demons: 6 + round progression + extra-player allowance.
/// - Health: +100 per round through Round 9 (150, 250 ... 950), then +10% per
///   round beginning with Round 10 (Round 10 is 1045).
/// - Spawn interval: multiply by 0.95 each round until the configured floor (0.4s).
/// - Walkers only in Round 1-2, Joggers eligible from Round 3, Runners from Round 5.
///
/// ADAPTATION
/// - High performance shortens spawn intervals, modestly raises active cap and
///   increases how much the round population grows.
/// - Low performance lengthens spawn intervals, lowers active cap, softens the
///   speed mix and slows future population growth.
/// - Health is round-based, not performance-based. This avoids rubber-banding HP.
/// - The adaptation changes the next wave plan only. It never deletes demons from
///   a wave that has already started. The Spawner must still spawn/track every one.
public sealed class WaveDirector : MonoBehaviour
{
    public static WaveDirector Instance { get; private set; }

    // Fires whenever PreviewWave creates a new plan.
    public event Action<WaveConfiguration> ConfigurationChanged;

    [Header("References")]
    [SerializeField] private TempoDirector tempoDirector;

    [Header("Preview Defaults")]
    [SerializeField, Min(1)] private int firstRoundToPreview = 1;
    [SerializeField, Range(1, 4)] private int defaultPlayerCount = 1;

    [Header("Population Baseline")]
    [Tooltip("Solo Round 1 population.")]
    [SerializeField, Min(1)] private int roundOneDemonCount = 6;
    [Tooltip("Base number added to the wave population per round after Round 1, before adaptation.")]
    [SerializeField, Min(0)] private int additionalDemonsPerRound = 2;
    [Tooltip("Fixed population allowance for each active player after the first. Tune from co-op playtests.")]
    [SerializeField, Min(0)] private int additionalDemonsPerExtraPlayer = 2;

    [Header("Adaptive Population Growth")]
    [Tooltip("At a Tempo Score of 0, use this proportion of normal round-to-round population growth.")]
    [SerializeField, Range(0.25f, 1f)] private float minimumPopulationGrowthMultiplier = 0.75f;
    [Tooltip("At a Tempo Score of 100, use this proportion of normal round-to-round population growth.")]
    [SerializeField, Range(1f, 2f)] private float maximumPopulationGrowthMultiplier = 1.35f;

    [Header("Active Demon Cap")]
    [SerializeField, Min(1)] private int baseActiveDemonCap = 4;
    [SerializeField, Min(0)] private int extraActiveCapPerPlayer = 2;
    [SerializeField, Min(1)] private int absoluteMaximumActiveDemons = 12;

    [Header("Spawn Timing")]
    [Tooltip("Round 1 gap between spawn batches, in seconds.")]
    [SerializeField, Min(0.1f)] private float roundOneSpawnInterval = 2f;
    [Tooltip("Compound reduction each round. 0.05 means multiply the previous interval by 0.95.")]
    [SerializeField, Range(0f, 0.5f)] private float spawnIntervalReductionPerRound = 0.05f;
    [Tooltip("The interval must never fall below this value, even at high rounds or with adaptation.")]
    [SerializeField, Min(0.1f)] private float minimumSpawnInterval = 0.4f;
    [SerializeField, Min(0.1f)] private float spawnRetryInterval = 1f;

    [Header("Spawn Batch Limits")]
    [SerializeField, Min(1)] private int baselineSpawnBatchLimit = 1;
    [SerializeField, Min(1)] private int sweeperSpawnBatchLimit = 2;
    [SerializeField, Min(1)] private int breathingRoomSpawnBatchLimit = 1;

    [Header("Health Progression")]
    [SerializeField, Min(1f)] private float roundOneDemonHealth = 150f;
    [Tooltip("Flat health added each round up to and including Round 9.")]
    [SerializeField, Min(0f)] private float healthIncreasePerRound = 100f;
    [Tooltip("Compound health increase from Round 10 onward. 0.1 means 10% per round.")]
    [SerializeField, Range(0f, 1f)] private float healthIncreasePercentageFromRound10 = 0.10f;

    [Header("Attack Damage Baseline")]
    [Tooltip("Damage from one standard demon swipe. It stays constant unless a separate damage-scaling rule is authored.")]
    [SerializeField, Min(0f)] private float demonSwipeDamage = 50f;

    [Header("Adaptation Thresholds")]
    [Tooltip("Scores below this use the Breathing Room profile; score 50 remains normal population growth.")]
    [SerializeField, Range(0f, 50f)] private float breathingRoomThreshold = 40f;
    [Tooltip("Scores above this use the Sweeper profile.")]
    [SerializeField, Range(50f, 100f)] private float sweeperThreshold = 60f;

    [Header("Sweeper Adjustments")]
    [Tooltip("Maximum spawn interval reduction at a score of 100.")]
    [SerializeField, Range(0f, 0.8f)] private float maximumSweeperIntervalReduction = 0.40f;
    [Tooltip("Maximum proportional increase to the active demon cap.")]
    [SerializeField, Range(0f, 1f)] private float maximumSweeperCapIncrease = 0.20f;

    [Header("Breathing Room Adjustments")]
    [Tooltip("Maximum proportional increase to the baseline spawn interval at a score of 0.")]
    [SerializeField, Range(0f, 2f)] private float maximumBreathingRoomIntervalIncrease = 0.25f;
    [Tooltip("Maximum proportional reduction to the active demon cap at a score of 0.")]
    [SerializeField, Range(0f, 0.8f)] private float maximumBreathingRoomCapReduction = 0.20f;

    [Header("Composition Unlocks")]
    [Tooltip("Joggers may be selected from this round onward.")]
    [SerializeField, Min(1)] private int joggerUnlockRound = 3;
    [Tooltip("Runners may be selected from this round onward. Performance never bypasses this unlock.")]
    [SerializeField, Min(1)] private int runnerUnlockRound = 5;

    [Header("Current Configuration (debug)")]
    [SerializeField] private WaveConfiguration currentConfiguration;

    public WaveConfiguration CurrentConfiguration => currentConfiguration;
    public int DefaultPlayerCount => defaultPlayerCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple WaveDirector instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (tempoDirector == null)
            tempoDirector = TempoDirector.Instance != null
                ? TempoDirector.Instance
                : FindFirstObjectByType<TempoDirector>();

        // Give the debug UI an initial configuration before a real wave manager exists.
        PreviewWave(firstRoundToPreview, defaultPlayerCount);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnValidate()
    {
        roundOneDemonCount = Mathf.Max(1, roundOneDemonCount);
        additionalDemonsPerRound = Mathf.Max(0, additionalDemonsPerRound);
        absoluteMaximumActiveDemons = Mathf.Max(1, absoluteMaximumActiveDemons);
        minimumSpawnInterval = Mathf.Max(0.1f, minimumSpawnInterval);
        roundOneSpawnInterval = Mathf.Max(minimumSpawnInterval, roundOneSpawnInterval);
        breathingRoomThreshold = Mathf.Clamp(breathingRoomThreshold, 0f, 50f);
        sweeperThreshold = Mathf.Clamp(sweeperThreshold, 50f, 100f);
        maximumPopulationGrowthMultiplier = Mathf.Max(1f, maximumPopulationGrowthMultiplier);
        minimumPopulationGrowthMultiplier = Mathf.Min(1f, minimumPopulationGrowthMultiplier);
        runnerUnlockRound = Mathf.Max(joggerUnlockRound, runnerUnlockRound);
    }

    // Builds and stores a plan for the requested round. The score used here is
    // from the most recently completed wave; on a fresh run it begins at 50.
    public WaveConfiguration PreviewWave(int roundNumber, int playerCount)
    {
        roundNumber = Mathf.Max(1, roundNumber);
        playerCount = Mathf.Clamp(playerCount, 1, 4);

        if (tempoDirector == null)
        {
            tempoDirector = TempoDirector.Instance;
            if (tempoDirector == null)
                tempoDirector = FindFirstObjectByType<TempoDirector>();
        }

        float performanceScore = tempoDirector != null ? tempoDirector.TempoScore : 50f;

        currentConfiguration = CreateConfiguration(roundNumber, playerCount, performanceScore);
        ConfigurationChanged?.Invoke(currentConfiguration);

        Debug.Log(
            $"Wave Director prepared Round {roundNumber}: " +
            $"{currentConfiguration.totalDemonCount} demons, " +
            $"cap {currentConfiguration.maximumActiveDemons}, " +
            $"interval {currentConfiguration.spawnInterval:F2}s, " +
            $"health {currentConfiguration.baseDemonHealth:F0}, " +
            $"swipe damage {currentConfiguration.baseDemonSwipeDamage:F0}, " +
            $"growth {currentConfiguration.populationGrowthMultiplier:F2}x, " +
            $"profile {currentConfiguration.adaptationProfile}."
        );

        return currentConfiguration;
    }

    // Builds a configuration from explicit inputs. This does not spawn enemies.
    public WaveConfiguration CreateConfiguration(int roundNumber, int playerCount, float performanceScore)
    {
        roundNumber = Mathf.Max(1, roundNumber);
        playerCount = Mathf.Clamp(playerCount, 1, 4);
        performanceScore = Mathf.Clamp(performanceScore, 0f, 100f);

        WaveAdaptationProfile profile = GetAdaptationProfile(performanceScore);
        float growthMultiplier = CalculatePopulationGrowthMultiplier(performanceScore);

        WaveConfiguration config = new WaveConfiguration
        {
            roundNumber = roundNumber,
            playerCount = playerCount,
            performanceScore = performanceScore,
            adaptationProfile = profile,
            populationGrowthMultiplier = growthMultiplier,
            baseDemonHealth = CalculateDemonHealth(roundNumber),
            baseDemonSwipeDamage = demonSwipeDamage,
            spawnRetryInterval = spawnRetryInterval
        };

        // Round 1 population is stable. Performance changes the rate of future
        // population growth, rather than removing enemies from a wave in progress.
        int playerAllowance = (playerCount - 1) * additionalDemonsPerExtraPlayer;
        int baselineRoundGrowth = Mathf.RoundToInt((roundNumber - 1) * additionalDemonsPerRound);
        int adaptedRoundGrowth = Mathf.RoundToInt(
            (roundNumber - 1) * additionalDemonsPerRound * growthMultiplier
        );

        config.baselineTotalDemonCount = roundOneDemonCount + baselineRoundGrowth + playerAllowance;
        config.totalDemonCount = Mathf.Max(1, roundOneDemonCount + adaptedRoundGrowth + playerAllowance);
        config.populationGrowthBonusDemons = config.totalDemonCount - config.baselineTotalDemonCount;

        // Base spawn spacing uses a compound 5% reduction per round by default.
        float roundInterval = roundOneSpawnInterval * Mathf.Pow(
            1f - spawnIntervalReductionPerRound,
            roundNumber - 1
        );
        roundInterval = Mathf.Max(minimumSpawnInterval, roundInterval);

        // A player-count baseline sets capacity; performance then adjusts it gently.
        int baselineCap = baseActiveDemonCap + (playerCount - 1) * extraActiveCapPerPlayer;
        baselineCap = Mathf.Clamp(baselineCap, 1, absoluteMaximumActiveDemons);

        config.spawnInterval = ApplySpawnIntervalProfile(roundInterval, performanceScore, profile);
        config.maximumActiveDemons = ApplyActiveCapProfile(baselineCap, performanceScore, profile);
        config.maximumActiveDemons = Mathf.Clamp(
            config.maximumActiveDemons,
            1,
            Mathf.Min(absoluteMaximumActiveDemons, config.totalDemonCount)
        );
        config.spawnBatchLimit = Mathf.Clamp(
            GetSpawnBatchLimit(profile),
            1,
            config.maximumActiveDemons
        );

        CalculateComposition(
            roundNumber,
            profile,
            out config.walkerPercentage,
            out config.joggerPercentage,
            out config.runnerPercentage
        );

        return config;
    }

    // Debug UI helper: preview the round after the current preview.
    public WaveConfiguration PreviewNextWave()
    {
        int nextRound = currentConfiguration != null
            ? currentConfiguration.roundNumber + 1
            : firstRoundToPreview;
        int players = currentConfiguration != null
            ? currentConfiguration.playerCount
            : defaultPlayerCount;

        return PreviewWave(nextRound, players);
    }

    private float CalculateDemonHealth(int roundNumber)
    {
        if (roundNumber <= 9)
        {
            return roundOneDemonHealth + ((roundNumber - 1) * healthIncreasePerRound);
        }

        float roundNineHealth = roundOneDemonHealth + (8f * healthIncreasePerRound);
        int roundsAfterNine = roundNumber - 9;
        return roundNineHealth * Mathf.Pow(1f + healthIncreasePercentageFromRound10, roundsAfterNine);
    }

    private float CalculatePopulationGrowthMultiplier(float score)
    {
        // Continuous mapping: 50 is normal growth, 0 eases future growth, 100
        // accelerates it. This runs independently from the discrete debug profile label.
        if (score < 50f)
        {
            float t = Mathf.InverseLerp(0f, 50f, score);
            return Mathf.Lerp(minimumPopulationGrowthMultiplier, 1f, t);
        }

        float highT = Mathf.InverseLerp(50f, 100f, score);
        return Mathf.Lerp(1f, maximumPopulationGrowthMultiplier, highT);
    }

    private WaveAdaptationProfile GetAdaptationProfile(float score)
    {
        if (score > sweeperThreshold)
            return WaveAdaptationProfile.Sweeper;
        if (score < breathingRoomThreshold)
            return WaveAdaptationProfile.BreathingRoom;
        return WaveAdaptationProfile.Baseline;
    }

    private float ApplySpawnIntervalProfile(float baselineInterval, float score, WaveAdaptationProfile profile)
    {
        if (profile == WaveAdaptationProfile.Sweeper)
        {
            float strength = Mathf.InverseLerp(sweeperThreshold, 100f, score);
            float reduction = maximumSweeperIntervalReduction * strength;
            return Mathf.Max(minimumSpawnInterval, baselineInterval * (1f - reduction));
        }

        if (profile == WaveAdaptationProfile.BreathingRoom)
        {
            float strength = 1f - Mathf.InverseLerp(0f, breathingRoomThreshold, score);
            float increase = maximumBreathingRoomIntervalIncrease * strength;
            return baselineInterval * (1f + increase);
        }

        return baselineInterval;
    }

    private int ApplyActiveCapProfile(int baselineCap, float score, WaveAdaptationProfile profile)
    {
        if (profile == WaveAdaptationProfile.Sweeper)
        {
            float strength = Mathf.InverseLerp(sweeperThreshold, 100f, score);
            int adjusted = Mathf.RoundToInt(baselineCap * (1f + maximumSweeperCapIncrease * strength));
            return Mathf.Clamp(adjusted, 1, absoluteMaximumActiveDemons);
        }

        if (profile == WaveAdaptationProfile.BreathingRoom)
        {
            float strength = 1f - Mathf.InverseLerp(0f, breathingRoomThreshold, score);
            int adjusted = Mathf.RoundToInt(baselineCap * (1f - maximumBreathingRoomCapReduction * strength));
            return Mathf.Max(1, adjusted);
        }

        return baselineCap;
    }

    private int GetSpawnBatchLimit(WaveAdaptationProfile profile)
    {
        switch (profile)
        {
            case WaveAdaptationProfile.Sweeper:
                return sweeperSpawnBatchLimit;
            case WaveAdaptationProfile.BreathingRoom:
                return breathingRoomSpawnBatchLimit;
            default:
                return baselineSpawnBatchLimit;
        }
    }

    private void CalculateComposition(
        int roundNumber,
        WaveAdaptationProfile profile,
        out int walkers,
        out int joggers,
        out int runners)
    {
        // Baseline movement mix. A type is never introduced before its unlock round.
        if (roundNumber < joggerUnlockRound)
        {
            walkers = 100;
            joggers = 0;
            runners = 0;
        }
        else if (roundNumber < runnerUnlockRound)
        {
            walkers = 80;
            joggers = 20;
            runners = 0;
        }
        else if (roundNumber < 10)
        {
            walkers = 80;
            joggers = 15;
            runners = 5;
        }
        else
        {
            walkers = 65;
            joggers = 25;
            runners = 10;
        }

        // Sweeper gets a modest faster mix; Breathing Room favours Walkers.
        // These are composition percentages, not a per-frame movement command.
        if (profile == WaveAdaptationProfile.Sweeper)
        {
            if (roundNumber >= 10)
            {
                walkers = 60;
                joggers = 28;
                runners = 12;
            }
            else if (roundNumber >= runnerUnlockRound)
            {
                walkers = 75;
                joggers = 20;
                runners = 5;
            }
            else if (roundNumber >= joggerUnlockRound)
            {
                walkers = 75;
                joggers = 25;
                runners = 0;
            }
        }
        else if (profile == WaveAdaptationProfile.BreathingRoom)
        {
            if (roundNumber >= runnerUnlockRound)
            {
                runners = Mathf.FloorToInt(runners * 0.5f);
                joggers = Mathf.FloorToInt(joggers * 0.75f);
                walkers = 100 - joggers - runners;
            }
            else if (roundNumber >= joggerUnlockRound)
            {
                joggers = Mathf.FloorToInt(joggers * 0.5f);
                runners = 0;
                walkers = 100 - joggers;
            }
        }

        joggers = Mathf.Clamp(joggers, 0, 100);
        runners = Mathf.Clamp(runners, 0, 100 - joggers);
        walkers = 100 - joggers - runners;
    }
}
