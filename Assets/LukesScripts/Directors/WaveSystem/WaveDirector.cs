using System;
using UnityEngine;

public enum WaveAdaptationProfile
{
    Sweeper,
    Baseline,
    BreathingRoom
}

// Runtime-only plan passed from WaveDirector to WaveSpawner and WavePerformanceRecorder.
[Serializable]
public sealed class WaveConfiguration
{
    public int roundNumber;
    public int playerCount;
    public int totalDemonCount;
    public int maximumActiveDemons;
    public int baselineTotalDemonCount;
    public int populationGrowthBonusDemons;
    public float populationGrowthMultiplier = 1f;
    public float spawnInterval;
    public int spawnBatchLimit;
    public float spawnRetryInterval;
    public float baseDemonHealth;
    public float baseDemonSwipeDamage;
    public int walkerPercentage;
    public int joggerPercentage;
    public int runnerPercentage;
    public WaveAdaptationProfile adaptationProfile;
    public float performanceScore;
}

// The sole Inspector owner for wave difficulty, scoring targets, learning,
// Tempo weights and intermission tuning. Other systems read these values
// through properties; they do not keep duplicate copies.
[DisallowMultipleComponent]
public sealed class WaveDirector : MonoBehaviour
{
    public static WaveDirector Instance { get; private set; }
    public event Action<WaveConfiguration> ConfigurationChanged;

    [Header("Starting Values")]
    [SerializeField, Min(1)] private int firstRoundToPreview = 1;
    [SerializeField, Range(1, 4)] private int defaultPlayerCount = 1;

    [Header("Population")]
    [SerializeField, Min(1)] private int roundOneDemonCount = 6;
    [SerializeField, Min(0)] private int additionalDemonsPerRound = 2;
    [SerializeField, Min(0)] private int additionalDemonsPerExtraPlayer = 2;
    [SerializeField, Range(0.25f, 1f)] private float minimumPopulationGrowthMultiplier = 0.75f;
    [SerializeField, Range(1f, 2f)] private float maximumPopulationGrowthMultiplier = 1.35f;

    [Header("Active Demon Limit")]
    [SerializeField, Min(1)] private int baseActiveDemonCap = 4;
    [SerializeField, Min(0)] private int extraActiveCapPerPlayer = 2;
    [SerializeField, Min(1)] private int absoluteMaximumActiveDemons = 12;

    [Header("Spawn Timing")]
    [SerializeField, Min(0.1f)] private float roundOneSpawnInterval = 2f;
    [SerializeField, Range(0f, 0.5f)] private float spawnIntervalReductionPerRound = 0.05f;
    [SerializeField, Min(0.1f)] private float minimumSpawnInterval = 0.4f;
    [SerializeField, Min(0.1f)] private float spawnRetryInterval = 1f;
    [SerializeField, Min(1)] private int baselineSpawnBatchLimit = 1;
    [SerializeField, Min(1)] private int sweeperSpawnBatchLimit = 2;
    [SerializeField, Min(1)] private int breathingRoomSpawnBatchLimit = 1;

    [Header("Demon Stats")]
    [SerializeField, Min(1f)] private float roundOneDemonHealth = 150f;
    [SerializeField, Min(0f)] private float healthIncreasePerRound = 100f;
    [SerializeField, Range(0f, 1f)] private float healthIncreasePercentageFromRound10 = 0.10f;
    [SerializeField, Min(0f)] private float demonSwipeDamage = 50f;

    [Header("Adaptation")]
    [SerializeField, Range(0f, 50f)] private float breathingRoomThreshold = 40f;
    [SerializeField, Range(50f, 100f)] private float sweeperThreshold = 60f;
    [SerializeField, Range(0f, 0.8f)] private float maximumSweeperIntervalReduction = 0.40f;
    [SerializeField, Range(0f, 1f)] private float maximumSweeperCapIncrease = 0.20f;
    [SerializeField, Range(0f, 2f)] private float maximumBreathingRoomIntervalIncrease = 0.25f;
    [SerializeField, Range(0f, 0.8f)] private float maximumBreathingRoomCapReduction = 0.20f;

    [Header("Enemy Type Unlocks")]
    [SerializeField, Min(1)] private int joggerUnlockRound = 3;
    [SerializeField, Min(1)] private int runnerUnlockRound = 5;

    [Header("Round 1 Performance Targets")]
    [SerializeField, Min(1f)] private float roundOneExpectedDuration = 40f;
    [SerializeField, Min(0.1f)] private float roundOneExpectedKillCadence = 5f;
    [SerializeField, Min(0.1f)] private float expectedFirstDamageTime = 5f;
    [SerializeField, Range(0f, 100f)] private float expectedMovementPercentage = 60f;
    [SerializeField, Min(0f)] private float roundOneExpectedDamageTaken = 150f;

    [Header("Points Calibration")]
    [Tooltip("Typical real points earned by an average player in solo Round 1, not the theoretical maximum.")]
    [SerializeField, Min(1f)] private float roundOneExpectedAveragePoints = 550f;
    [Tooltip("Tempo metric score assigned when the expected average points target is reached.")]
    [SerializeField, Range(1f, 100f)] private float pointsScoreAtExpectedAverage = 75f;

    [Header("Workload Scaling")]
    [SerializeField, Range(0f, 1f)] private float healthWorkloadExponent = 0.5f;
    [SerializeField, Range(0f, 1f)] private float playerWorkloadExponent = 0.5f;
    [SerializeField, Range(0f, 1.5f)] private float damageExposureExponent = 0.75f;
    [SerializeField, Range(0f, 1.5f)] private float swipeDamageExponent = 0.75f;

    [Header("Baseline Learning")]
    [SerializeField, Range(0.01f, 1f)] private float learningRate = 0.20f;
    [SerializeField, Range(1f, 10f)] private float maximumObservedRatio = 4f;

    [Header("Tempo Score")]
    [SerializeField] private bool useScoreSmoothing = true;
    [SerializeField, Range(0f, 1f)] private float previousScoreInfluence = 0.25f;
    [SerializeField, Min(0f)] private float roundTimeWeight = 0.25f;
    [SerializeField, Min(0f)] private float killCadenceWeight = 0.30f;
    [SerializeField, Min(0f)] private float firstDamageWeight = 0.05f;
    [SerializeField, Min(0f)] private float movementWeight = 0.05f;
    [SerializeField, Min(0f)] private float damageTakenWeight = 0.15f;
    [SerializeField, Min(0f)] private float damageDealtWeight = 0.10f;
    [SerializeField, Min(0f)] private float pointsWeight = 0.10f;

    [Header("Intermission")]
    [SerializeField, Min(0f)] private float minimumIntermission = 5f;
    [SerializeField, Min(0f)] private float defaultIntermission = 12f;
    [SerializeField, Min(0f)] private float maximumIntermission = 25f;

    private TempoDirector tempoDirector;
    private WaveConfiguration currentConfiguration;

    public WaveConfiguration CurrentConfiguration => currentConfiguration;
    public int FirstRoundToStart => firstRoundToPreview;
    public int DefaultPlayerCount => defaultPlayerCount;
    public float DefaultIntermission => defaultIntermission;

    // Read-only tuning API used by WaveBaselineManager and TempoDirector.
    public int RoundOneDemonCount => roundOneDemonCount;
    public int AdditionalDemonsPerRound => additionalDemonsPerRound;
    public int AdditionalDemonsPerExtraPlayer => additionalDemonsPerExtraPlayer;
    public int BaseActiveDemonCap => baseActiveDemonCap;
    public int ExtraActiveCapPerPlayer => extraActiveCapPerPlayer;
    public int AbsoluteMaximumActiveDemons => absoluteMaximumActiveDemons;
    public float RoundOneSpawnInterval => roundOneSpawnInterval;
    public float RoundOneDemonHealth => roundOneDemonHealth;
    public float HealthIncreasePerRound => healthIncreasePerRound;
    public float HealthIncreasePercentageFromRound10 => healthIncreasePercentageFromRound10;
    public float DemonSwipeDamage => demonSwipeDamage;
    public float RoundOneExpectedDuration => roundOneExpectedDuration;
    public float RoundOneExpectedKillCadence => roundOneExpectedKillCadence;
    public float ExpectedFirstDamageTime => expectedFirstDamageTime;
    public float ExpectedMovementPercentage => expectedMovementPercentage;
    public float RoundOneExpectedDamageTaken => roundOneExpectedDamageTaken;
    public float RoundOneExpectedAveragePoints => roundOneExpectedAveragePoints;
    public float PointsScoreAtExpectedAverage => pointsScoreAtExpectedAverage;
    public float HealthWorkloadExponent => healthWorkloadExponent;
    public float PlayerWorkloadExponent => playerWorkloadExponent;
    public float DamageExposureExponent => damageExposureExponent;
    public float SwipeDamageExponent => swipeDamageExponent;
    public float LearningRate => learningRate;
    public float MaximumObservedRatio => maximumObservedRatio;
    public float MinimumIntermission => minimumIntermission;
    public float DefaultIntermissionSeconds => defaultIntermission;
    public float MaximumIntermission => maximumIntermission;
    public bool UseScoreSmoothing => useScoreSmoothing;
    public float PreviousScoreInfluence => previousScoreInfluence;
    public float RoundTimeWeight => roundTimeWeight;
    public float KillCadenceWeight => killCadenceWeight;
    public float FirstDamageWeight => firstDamageWeight;
    public float MovementWeight => movementWeight;
    public float DamageTakenWeight => damageTakenWeight;
    public float DamageDealtWeight => damageDealtWeight;
    public float PointsWeight => pointsWeight;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple WaveDirector instances detected. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ResolveTempoDirector();
        PreviewWave(firstRoundToPreview, defaultPlayerCount);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnValidate()
    {
        firstRoundToPreview = Mathf.Max(1, firstRoundToPreview);
        defaultPlayerCount = Mathf.Clamp(defaultPlayerCount, 1, 4);
        roundOneDemonCount = Mathf.Max(1, roundOneDemonCount);
        additionalDemonsPerRound = Mathf.Max(0, additionalDemonsPerRound);
        additionalDemonsPerExtraPlayer = Mathf.Max(0, additionalDemonsPerExtraPlayer);
        baseActiveDemonCap = Mathf.Max(1, baseActiveDemonCap);
        extraActiveCapPerPlayer = Mathf.Max(0, extraActiveCapPerPlayer);
        absoluteMaximumActiveDemons = Mathf.Max(1, absoluteMaximumActiveDemons);
        minimumSpawnInterval = Mathf.Max(0.1f, minimumSpawnInterval);
        roundOneSpawnInterval = Mathf.Max(minimumSpawnInterval, roundOneSpawnInterval);
        spawnRetryInterval = Mathf.Max(0.1f, spawnRetryInterval);
        roundOneDemonHealth = Mathf.Max(1f, roundOneDemonHealth);
        healthIncreasePerRound = Mathf.Max(0f, healthIncreasePerRound);
        demonSwipeDamage = Mathf.Max(0f, demonSwipeDamage);
        breathingRoomThreshold = Mathf.Clamp(breathingRoomThreshold, 0f, 50f);
        sweeperThreshold = Mathf.Clamp(sweeperThreshold, 50f, 100f);
        minimumPopulationGrowthMultiplier = Mathf.Clamp(minimumPopulationGrowthMultiplier, 0.25f, 1f);
        maximumPopulationGrowthMultiplier = Mathf.Max(1f, maximumPopulationGrowthMultiplier);
        joggerUnlockRound = Mathf.Max(1, joggerUnlockRound);
        runnerUnlockRound = Mathf.Max(joggerUnlockRound, runnerUnlockRound);
        roundOneExpectedDuration = Mathf.Max(1f, roundOneExpectedDuration);
        roundOneExpectedKillCadence = Mathf.Max(0.1f, roundOneExpectedKillCadence);
        expectedFirstDamageTime = Mathf.Max(0.1f, expectedFirstDamageTime);
        roundOneExpectedAveragePoints = Mathf.Max(1f, roundOneExpectedAveragePoints);
        pointsScoreAtExpectedAverage = Mathf.Clamp(pointsScoreAtExpectedAverage, 1f, 100f);
        learningRate = Mathf.Clamp(learningRate, 0.01f, 1f);
        maximumObservedRatio = Mathf.Max(1f, maximumObservedRatio);
        previousScoreInfluence = Mathf.Clamp01(previousScoreInfluence);
        minimumIntermission = Mathf.Max(0f, minimumIntermission);
        maximumIntermission = Mathf.Max(minimumIntermission, maximumIntermission);
        defaultIntermission = Mathf.Clamp(defaultIntermission, minimumIntermission, maximumIntermission);
    }

    public WaveConfiguration PreviewWave(int roundNumber, int playerCount)
    {
        ResolveTempoDirector();
        float score = tempoDirector != null ? tempoDirector.TempoScore : 50f;

        currentConfiguration = CreateConfiguration(roundNumber, playerCount, score);
        ConfigurationChanged?.Invoke(currentConfiguration);

        Debug.Log(
            $"Wave Director prepared Round {currentConfiguration.roundNumber}: " +
            $"{currentConfiguration.totalDemonCount} demons, cap {currentConfiguration.maximumActiveDemons}, " +
            $"interval {currentConfiguration.spawnInterval:F2}s, HP {currentConfiguration.baseDemonHealth:F0}, " +
            $"swipe {currentConfiguration.baseDemonSwipeDamage:F0}, profile {currentConfiguration.adaptationProfile}.",
            this);

        return currentConfiguration;
    }

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

        int playerAllowance = (playerCount - 1) * additionalDemonsPerExtraPlayer;
        int baselineGrowth = Mathf.RoundToInt((roundNumber - 1) * additionalDemonsPerRound);
        int adaptedGrowth = Mathf.RoundToInt((roundNumber - 1) * additionalDemonsPerRound * growthMultiplier);

        config.baselineTotalDemonCount = roundOneDemonCount + baselineGrowth + playerAllowance;
        config.totalDemonCount = Mathf.Max(1, roundOneDemonCount + adaptedGrowth + playerAllowance);
        config.populationGrowthBonusDemons = config.totalDemonCount - config.baselineTotalDemonCount;

        float interval = roundOneSpawnInterval * Mathf.Pow(1f - spawnIntervalReductionPerRound, roundNumber - 1);
        interval = Mathf.Max(minimumSpawnInterval, interval);

        int baselineCap = baseActiveDemonCap + (playerCount - 1) * extraActiveCapPerPlayer;
        baselineCap = Mathf.Clamp(baselineCap, 1, absoluteMaximumActiveDemons);

        config.spawnInterval = ApplySpawnIntervalProfile(interval, performanceScore, profile);
        config.maximumActiveDemons = ApplyActiveCapProfile(baselineCap, performanceScore, profile);
        config.maximumActiveDemons = Mathf.Clamp(config.maximumActiveDemons, 1,
            Mathf.Min(absoluteMaximumActiveDemons, config.totalDemonCount));
        config.spawnBatchLimit = Mathf.Clamp(GetSpawnBatchLimit(profile), 1, config.maximumActiveDemons);

        CalculateComposition(roundNumber, profile,
            out config.walkerPercentage, out config.joggerPercentage, out config.runnerPercentage);

        return config;
    }

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

    private void ResolveTempoDirector()
    {
        if (tempoDirector != null)
            return;
        tempoDirector = TempoDirector.Instance != null
            ? TempoDirector.Instance
            : FindFirstObjectByType<TempoDirector>();
    }

    private float CalculateDemonHealth(int roundNumber)
    {
        if (roundNumber <= 9)
            return roundOneDemonHealth + (roundNumber - 1) * healthIncreasePerRound;

        float roundNineHealth = roundOneDemonHealth + 8f * healthIncreasePerRound;
        return roundNineHealth * Mathf.Pow(1f + healthIncreasePercentageFromRound10, roundNumber - 9);
    }

    private float CalculatePopulationGrowthMultiplier(float score)
    {
        if (score < 50f)
            return Mathf.Lerp(minimumPopulationGrowthMultiplier, 1f, Mathf.InverseLerp(0f, 50f, score));
        return Mathf.Lerp(1f, maximumPopulationGrowthMultiplier, Mathf.InverseLerp(50f, 100f, score));
    }

    private WaveAdaptationProfile GetAdaptationProfile(float score)
    {
        if (score > sweeperThreshold) return WaveAdaptationProfile.Sweeper;
        if (score < breathingRoomThreshold) return WaveAdaptationProfile.BreathingRoom;
        return WaveAdaptationProfile.Baseline;
    }

    private float ApplySpawnIntervalProfile(float baseline, float score, WaveAdaptationProfile profile)
    {
        if (profile == WaveAdaptationProfile.Sweeper)
        {
            float strength = Mathf.InverseLerp(sweeperThreshold, 100f, score);
            return Mathf.Max(minimumSpawnInterval, baseline * (1f - maximumSweeperIntervalReduction * strength));
        }

        if (profile == WaveAdaptationProfile.BreathingRoom)
        {
            float strength = 1f - Mathf.InverseLerp(0f, breathingRoomThreshold, score);
            return baseline * (1f + maximumBreathingRoomIntervalIncrease * strength);
        }

        return baseline;
    }

    private int ApplyActiveCapProfile(int baseline, float score, WaveAdaptationProfile profile)
    {
        if (profile == WaveAdaptationProfile.Sweeper)
        {
            float strength = Mathf.InverseLerp(sweeperThreshold, 100f, score);
            return Mathf.Clamp(Mathf.RoundToInt(baseline * (1f + maximumSweeperCapIncrease * strength)),
                1, absoluteMaximumActiveDemons);
        }

        if (profile == WaveAdaptationProfile.BreathingRoom)
        {
            float strength = 1f - Mathf.InverseLerp(0f, breathingRoomThreshold, score);
            return Mathf.Max(1, Mathf.RoundToInt(baseline * (1f - maximumBreathingRoomCapReduction * strength)));
        }

        return baseline;
    }

    private int GetSpawnBatchLimit(WaveAdaptationProfile profile)
    {
        switch (profile)
        {
            case WaveAdaptationProfile.Sweeper: return sweeperSpawnBatchLimit;
            case WaveAdaptationProfile.BreathingRoom: return breathingRoomSpawnBatchLimit;
            default: return baselineSpawnBatchLimit;
        }
    }

    private void CalculateComposition(int roundNumber, WaveAdaptationProfile profile,
        out int walkers, out int joggers, out int runners)
    {
        if (roundNumber < joggerUnlockRound)
        {
            walkers = 100; joggers = 0; runners = 0;
        }
        else if (roundNumber < runnerUnlockRound)
        {
            walkers = 80; joggers = 20; runners = 0;
        }
        else if (roundNumber < 10)
        {
            walkers = 80; joggers = 15; runners = 5;
        }
        else
        {
            walkers = 65; joggers = 25; runners = 10;
        }

        if (profile == WaveAdaptationProfile.Sweeper)
        {
            if (roundNumber >= 10) { walkers = 60; joggers = 28; runners = 12; }
            else if (roundNumber >= runnerUnlockRound) { walkers = 75; joggers = 20; runners = 5; }
            else if (roundNumber >= joggerUnlockRound) { walkers = 75; joggers = 25; runners = 0; }
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
