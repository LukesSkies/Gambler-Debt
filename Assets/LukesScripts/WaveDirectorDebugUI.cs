using TMPro;
using UnityEngine;

public sealed class WaveDirectorDebugUI : MonoBehaviour
{
    private WaveDirector waveDirector;

    [Header("Wave Population")]
    [SerializeField] private TMP_Text totalDemonsText;
    [SerializeField] private TMP_Text maximumActiveDemonsText;
    [SerializeField] private TMP_Text demonHealthText;
    [SerializeField] private TMP_Text demonSwipeDamageText;

    [Header("Population Scaling")]
    [SerializeField] private TMP_Text populationGrowthMultiplierText;
    [SerializeField] private TMP_Text populationGrowthBonusDemonsText;

    [Header("Spawn Timing")]
    [SerializeField] private TMP_Text spawnIntervalText;
    [SerializeField] private TMP_Text spawnBatchLimitText;
    [SerializeField] private TMP_Text spawnRetryIntervalText;

    [Header("Demon Composition")]
    [SerializeField] private TMP_Text walkersText;
    [SerializeField] private TMP_Text joggersText;
    [SerializeField] private TMP_Text runnersText;

    private void OnEnable()
    {
        SubscribeToWaveDirector();
    }

    private void Start()
    {
        // Start runs after all scene Awakes, so it can also handle auto-discovery.
        SubscribeToWaveDirector();
        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (waveDirector != null)
            waveDirector.ConfigurationChanged -= OnConfigurationChanged;
    }

    // Refreshes the fields from the latest planned configuration.
    public void RefreshDisplay()
    {
        if (!FindWaveDirector())
            return;

        WaveConfiguration config = waveDirector.CurrentConfiguration;
        if (config == null)
            return;

        if (totalDemonsText != null)
            totalDemonsText.text = config.totalDemonCount.ToString();

        if (maximumActiveDemonsText != null)
            maximumActiveDemonsText.text = config.maximumActiveDemons.ToString();

        if (demonHealthText != null)
            demonHealthText.text = config.baseDemonHealth.ToString("F0");

        if (demonSwipeDamageText != null)
            demonSwipeDamageText.text = config.baseDemonSwipeDamage.ToString("F0");

        if (populationGrowthMultiplierText != null)
            populationGrowthMultiplierText.text = $"{config.populationGrowthMultiplier:F2}x";

        if (populationGrowthBonusDemonsText != null)
            populationGrowthBonusDemonsText.text =
                config.populationGrowthBonusDemons >= 0
                    ? $"+{config.populationGrowthBonusDemons}"
                    : config.populationGrowthBonusDemons.ToString();

        if (spawnIntervalText != null)
            spawnIntervalText.text = config.spawnInterval.ToString("F2");

        if (spawnBatchLimitText != null)
            spawnBatchLimitText.text = config.spawnBatchLimit.ToString();

        if (spawnRetryIntervalText != null)
            spawnRetryIntervalText.text = config.spawnRetryInterval.ToString("F2");

        if (walkersText != null)
            walkersText.text = $"{config.walkerPercentage}%";

        if (joggersText != null)
            joggersText.text = $"{config.joggerPercentage}%";

        if (runnersText != null)
            runnersText.text = $"{config.runnerPercentage}%";
    }

    // Optional debug-button callback. This previews only; it does not start a wave.
    public void PreviewNextWave()
    {
        if (!FindWaveDirector())
            return;

        waveDirector.PreviewNextWave();
        RefreshDisplay();
    }

    private void OnConfigurationChanged(WaveConfiguration configuration)
    {
        RefreshDisplay();
    }

    private void SubscribeToWaveDirector()
    {
        if (!FindWaveDirector())
            return;

        waveDirector.ConfigurationChanged -= OnConfigurationChanged;
        waveDirector.ConfigurationChanged += OnConfigurationChanged;
    }

    private bool FindWaveDirector()
    {
        if (waveDirector == null)
        {
            waveDirector = WaveDirector.Instance != null
                ? WaveDirector.Instance
                : FindFirstObjectByType<WaveDirector>();
        }

        return waveDirector != null;
    }
}
