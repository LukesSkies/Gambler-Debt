using TMPro;
using UnityEngine;

/// This component reads the configuration currently stored by WaveDirector and
/// copies selected values into TextMeshPro fields. It does not create a wave,
/// spawn enemies, count living enemies, or change the configuration itself.
///
/// HOW IT CONNECTS TO THE CURRENT WAVE DIRECTOR:
/// - The supplied WaveDirector exposes CurrentConfiguration.
/// - The supplied WaveDirector raises ConfigurationChanged whenever PreviewWave
///   creates a new configuration.
/// - This component subscribes to that event and refreshes the displayed values.
///
/// WHAT THE REAL GAME STILL NEEDS:
/// - The production WaveManager must request a configuration from WaveDirector.
/// - The WaveManager must pass that same configuration to the real WaveSpawner.
/// - The WaveSpawner must actually apply the count, cap, interval, batch limit,
///   health, swipe damage, and Walker/Jogger/Runner composition.
/// - The WavePerformanceRecorder must receive that same configuration in
///   BeginWave(configuration), so TempoDirector evaluates the workload that was
///   actually played rather than a preview or different set of settings.
///
/// IMPORTANT: the supplied WaveDirector.Start currently calls PreviewWave to
/// create an initial preview for debugging. PreviewWave only prepares data; it
/// does not start gameplay. Do not use PreviewNextWave as your production round
/// progression method. The real WaveManager should own round progression.
public sealed class WaveDirectorDebugUI : MonoBehaviour
{
    [Header("Reference")]
    [Tooltip("The WaveDirector being displayed. Assign the scene's WaveDirector here, or leave empty to use its singleton/scene lookup.")]
    [SerializeField] private WaveDirector waveDirector;

    [Header("Wave Population")]
    [Tooltip("The total number of demons planned for the entire wave. This is not the current number alive.")]
    [SerializeField] private TMP_Text totalDemonsText;
    [Tooltip("The maximum number of demons the WaveSpawner should allow to be active at the same time.")]
    [SerializeField] private TMP_Text maximumActiveDemonsText;
    [Tooltip("The base health that the real spawner/health component must assign to each standard demon created for this wave.")]
    [SerializeField] private TMP_Text demonHealthText;
    [Tooltip("The damage that the real demon attack component must apply for one standard swipe. The debug UI does not apply this damage.")]
    [SerializeField] private TMP_Text demonSwipeDamageText;

    [Header("Population Scaling")]
    [Tooltip("The growth multiplier used by WaveDirector when calculating round population growth. 1.00x means normal growth.")]
    [SerializeField] private TMP_Text populationGrowthMultiplierText;
    [Tooltip("The difference between the adapted total demon count and the non-adapted baseline count.")]
    [SerializeField] private TMP_Text populationGrowthBonusDemonsText;

    [Header("Spawn Timing")]
    [Tooltip("The delay between spawn batches. The production WaveSpawner must read and use this value.")]
    [SerializeField] private TMP_Text spawnIntervalText;
    [Tooltip("The maximum number of demons the production WaveSpawner should attempt to create in one batch.")]
    [SerializeField] private TMP_Text spawnBatchLimitText;
    [Tooltip("The delay the production WaveSpawner should use before retrying a failed/blocked spawn attempt.")]
    [SerializeField] private TMP_Text spawnRetryIntervalText;

    [Header("Demon Composition")]
    [Tooltip("The percentage of this wave intended to use Walker movement behaviour. The production spawner must choose/configure the correct demon prefab or AI mode.")]
    [SerializeField] private TMP_Text walkersText;
    [Tooltip("The percentage of this wave intended to use Jogger movement behaviour. The production spawner must respect the unlock round in WaveDirector.")]
    [SerializeField] private TMP_Text joggersText;
    [Tooltip("The percentage of this wave intended to use Runner movement behaviour. The production spawner must respect the unlock round in WaveDirector.")]
    [SerializeField] private TMP_Text runnersText;


    // Unity calls this when the UI object becomes enabled. We find the current
    // WaveDirector and subscribe to its ConfigurationChanged event. If the
    // director has not run Awake yet, Start performs another lookup.
    //
    // LIVE CONNECTION: the supplied WaveDirector already has the matching event,
    // so no change is needed to that event for this UI to receive updates.
    // If the game later changes scenes/additively loads managers, reconnect this
    // UI when the active WaveDirector instance changes.

    private void OnEnable()
    {
        SubscribeToWaveDirector();
    }


    // Performs a second subscription attempt after scene Awake calls have run,
    // then displays the director's current configuration if one exists.
    // Unsubscribing before subscribing prevents duplicate event registrations.

    private void Start()
    {
        SubscribeToWaveDirector();
        RefreshDisplay();
    }


    // Removes this UI from the director's event when disabled. This prevents a
    // still-running WaveDirector from calling a disabled UI component.
    // If the production game replaces WaveDirector at runtime, the replacement
    // director must also be subscribed when this UI is enabled again.

    private void OnDisable()
    {
        if (waveDirector != null)
            waveDirector.ConfigurationChanged -= OnConfigurationChanged;
    }


    // Reads WaveDirector.CurrentConfiguration and updates every assigned TMP field.
    // Unassigned TMP references are skipped, so this UI can display only the fields
    // needed by the current debug Canvas.
    //
    // LIVE CONNECTION:
    // The supplied WaveDirector already exposes CurrentConfiguration and the
    // WaveConfiguration field names used here. If those fields are renamed, update
    // the matching reads below. If you need live spawned/alive counts, those must
    // come from WaveSpawner; this configuration only contains planned values.

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
        {
            populationGrowthBonusDemonsText.text =
                config.populationGrowthBonusDemons >= 0
                    ? $"+{config.populationGrowthBonusDemons}"
                    : config.populationGrowthBonusDemons.ToString();
        }

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


    // DEBUG-ONLY BUTTON CALLBACK. It asks the supplied WaveDirector to prepare
    // the round after its current preview, then refreshes this display.
    //
    // LIVE GAME CHANGE:
    // Do not connect this method to the production Start Next Wave button.
    // The real WaveManager must decide when a round starts, call
    // WaveDirector.PreviewWave(nextRound, playerCount) (or a renamed production
    // configuration method), then pass the returned configuration to both the
    // WaveSpawner and WavePerformanceRecorder. Remove this debug button from
    // the shipped HUD if players should not be able to change the preview.

    public void PreviewNextWave()
    {
        if (!FindWaveDirector())
            return;

        waveDirector.PreviewNextWave();
        RefreshDisplay();
    }


    // Called by WaveDirector.ConfigurationChanged after it creates a new plan.
    // The configuration parameter is the plan that triggered the event; this
    // method refreshes from CurrentConfiguration so the display always reads
    // the director's currently stored plan.

    private void OnConfigurationChanged(WaveConfiguration configuration)
    {
        RefreshDisplay();
    }


    // Finds the director reference and subscribes to its configuration event.
    // A serialized Inspector reference is preferred; otherwise the singleton
    // is used, followed by a scene search.
    //
    // LIVE CONNECTION:
    // The supplied WaveDirector has a singleton and ConfigurationChanged event.
    // If production code stops using a singleton or creates multiple wave systems,
    // replace this lookup with an explicit reference supplied by WaveManager.

    private void SubscribeToWaveDirector()
    {
        if (!FindWaveDirector())
            return;

        waveDirector.ConfigurationChanged -= OnConfigurationChanged;
        waveDirector.ConfigurationChanged += OnConfigurationChanged;
    }


    // Returns true if the director reference is valid. This method does not create
    // a director; it only finds one already present in the scene.

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
