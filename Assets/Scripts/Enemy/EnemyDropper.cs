using UnityEngine;

public class EnemyDropper : MonoBehaviour
{
    [System.Serializable]
    public class DropOption
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float dropChance = 0.25f;
    }

    [Header("Drop Settings")]
    [Range(0f, 1f)] public float globalDropChance = 0.3f;
    public DropOption[] possibleDrops;
    public float spawnHeightOffset = 0.5f;
    public float scatterRadius = 0.5f;

    public void DropLoot()
    {
        if (Random.value > globalDropChance)
        {
            Debug.Log("No loot dropped this time.");
            return;
        }

        DropOption chosenDrop = GetWeightedRandomDrop();
        if (chosenDrop == null || chosenDrop.prefab == null)
        {
            Debug.Log("No valid drop chosen.");
            return;
        }

        Vector3 spawnPos = transform.position + Vector3.up * spawnHeightOffset;
        spawnPos += new Vector3(
            Random.Range(-scatterRadius, scatterRadius),
            0f,
            Random.Range(-scatterRadius, scatterRadius)
        );

        Instantiate(chosenDrop.prefab, spawnPos, Quaternion.identity);
        Debug.Log($"Dropped {chosenDrop.prefab.name}");
    }

    private DropOption GetWeightedRandomDrop()
    {
        float totalWeight = 0f;
        foreach (var d in possibleDrops)
            totalWeight += d.dropChance;

        float roll = Random.value * totalWeight;
        float running = 0f;

        foreach (var d in possibleDrops)
        {
            running += d.dropChance;
            if (roll <= running)
                return d;
        }

        return null;
    }
}
