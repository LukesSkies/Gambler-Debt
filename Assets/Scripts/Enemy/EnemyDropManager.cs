using UnityEngine;

public class EnemyDropSpawner : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Drop Settings")]
    [Tooltip("Possible drop prefabs (coin, health, shield, ammo)")]
    public GameObject[] dropPrefabs;

    [Range(0f, 1f)]
    [Tooltip("Chance that any drop will spawn on death (e.g. 0.25 = 25%)")]
    public float dropChance = 0.25f;

    [Tooltip("Vertical offset for where drops appear above the ground.")]
    public float spawnHeightOffset = 0.5f;

    [Tooltip("Maximum random horizontal scatter when spawning drops.")]
    public float spawnScatterRadius = 0.5f;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        TrySpawnDrop();
        Destroy(gameObject);
    }

    void TrySpawnDrop()
    {
        if (dropPrefabs == null || dropPrefabs.Length == 0)
            return;

        // 🎲 Check if drop happens
        if (Random.value > dropChance)
            return;

        // Pick a random drop type
        GameObject dropPrefab = dropPrefabs[Random.Range(0, dropPrefabs.Length)];
        if (!dropPrefab) return;

        // Random spawn position offset
        Vector3 spawnPos = transform.position + Vector3.up * spawnHeightOffset;
        spawnPos += new Vector3(Random.Range(-spawnScatterRadius, spawnScatterRadius), 0, Random.Range(-spawnScatterRadius, spawnScatterRadius));

        // Instantiate safely — drops are not destroyed because this happens before Destroy(gameObject)
        Instantiate(dropPrefab, spawnPos, Quaternion.identity);

        Debug.Log($"🪙 Spawned drop: {dropPrefab.name} at {spawnPos}");
    }
}
