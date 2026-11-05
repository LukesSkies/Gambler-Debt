using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class WavePlayerRecorderBridge : MonoBehaviour
{
    private const float MovementSpeedThreshold = 0.1f;

    private PlayerMove playerMove;
    private PlayerHP playerHealth;
    private Points points;
    private Rigidbody body;
    private WavePerformanceRecorder recorder;

    private float previousHealth;
    private int previousPoints;
    private bool initialized;
    private int resolvedPlayerId;

    public int PlayerId => resolvedPlayerId;

    private void Awake()
    {
        CacheReferences();
    }

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        CacheReferences();

        if (playerHealth != null)
            previousHealth = playerHealth.Health;

        if (points != null)
            previousPoints = points.Money;

        recorder = WavePerformanceRecorder.Instance;
        resolvedPlayerId = ResolvePlayerId();
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            Initialize();

        // Refreshing the singleton reference also supports scene reloads where
        // the Recorder instance is replaced.
        if (recorder == null)
            recorder = WavePerformanceRecorder.Instance;

        if (playerHealth != null)
        {
            float currentHealth = playerHealth.Health;
            float healthLost = previousHealth - currentHealth;

            if (healthLost > 0f && recorder != null)
                recorder.RecordDamageTaken(healthLost);

            // Updating this every frame prevents healing/reviving between waves
            // from being counted as damage when the next wave begins.
            previousHealth = currentHealth;
        }

        if (points != null)
        {
            int currentPoints = points.Money;
            int pointsGained = currentPoints - previousPoints;

            // This observes both Points.AddPoints and existing direct positive
            // Money changes, including the current Bullet script. Purchases and
            // other deductions are ignored.
            if (pointsGained > 0 && recorder != null)
                recorder.RecordPointsEarned(pointsGained);

            previousPoints = currentPoints;
        }

        if (recorder != null && playerMove != null)
        {
            bool isMoving = IsActuallyMoving();
            recorder.RecordMovement(isMoving, Time.deltaTime);
        }
    }

    private void CacheReferences()
    {
        if (playerMove == null)
            playerMove = GetComponent<PlayerMove>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHP>();

        if (points == null)
            points = GetComponent<Points>();

        if (body == null)
            body = GetComponent<Rigidbody>();
    }

    private bool IsActuallyMoving()
    {
        if (body == null)
            return false;

        Vector3 horizontalVelocity = body.linearVelocity;
        horizontalVelocity.y = 0f;
        return horizontalVelocity.magnitude > MovementSpeedThreshold;
    }

    private int ResolvePlayerId()
    {
        PlayerInput input = GetComponent<PlayerInput>();
        if (input != null && input.playerIndex >= 0)
            return input.playerIndex;

        const string playerPrefix = "NewPlayer";
        if (gameObject.name.StartsWith(playerPrefix) &&
            int.TryParse(gameObject.name.Substring(playerPrefix.Length), out int parsedId))
        {
            return parsedId;
        }

        return 0;
    }
}
