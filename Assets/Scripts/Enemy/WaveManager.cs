using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class WaveSpawner : MonoBehaviour
{
    [Header("Wave Settings")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int baseEnemies = 7;
    public int maxEnemiesPerWave = 20;
    public float waveDelay = 10f;
    public float spawnRadius = 8f;
    public float waveTimeLimit = 30f;

    [Header("Difficulty Scaling")]
    public float healthIncrease = 1.2f;
    public float damageIncrease = 1.1f;
    public float countIncrease = 1.15f;

    [Header("UI")]
    public TextMeshProUGUI waveText;

    private int currentWave = 0;
    private List<GameObject> aliveEnemies = new List<GameObject>();
    private float waveStartTime;
    private bool spawningWave = false;

    void Start()
    {
        if (waveText)
            waveText.text = "WAVE 0";

        StartCoroutine(WaveLoop());
    }

    IEnumerator WaveLoop()
    {
        while (true)
        {
            currentWave++;
            UpdateWaveUI();
            SpawnWave();

            waveStartTime = Time.time;

            // Wait until enemies die
            yield return new WaitUntil(() => AllEnemiesCleared());

            // Award bonus immediately
            CheckPerfectWaveRewards();

            float waveDuration = Time.time - waveStartTime;
            float nextDelay = waveDelay;

            if (waveDuration < waveTimeLimit * 0.5f)
                nextDelay *= 0.5f;
            else if (waveDuration > waveTimeLimit)
                nextDelay *= 1.5f;

            Debug.Log($"Wave {currentWave} cleared in {waveDuration:F1}s. Next wave in {nextDelay:F1}s");

            yield return new WaitForSeconds(nextDelay);
        }
    }

    void SpawnWave()
    {
        if (enemyPrefab == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("WaveSpawner missing prefab or spawn points!");
            return;
        }

        spawningWave = true;

        int enemyCount = Mathf.RoundToInt(baseEnemies * Mathf.Pow(countIncrease, currentWave / 3f));
        enemyCount = Mathf.Clamp(enemyCount, 1, maxEnemiesPerWave);

        for (int i = 0; i < enemyCount; i++)
        {
            Transform chosenSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

            Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
            randomOffset.y = 0f;
            Vector3 spawnPos = chosenSpawn.position + randomOffset;

            GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            newEnemy.tag = "Enemy";

            EnemyAI ai = newEnemy.GetComponent<EnemyAI>();
            if (ai)
            {
                ai.maxHealth *= Mathf.Pow(healthIncrease, currentWave / 3f);
                ai.damage *= Mathf.Pow(damageIncrease, currentWave / 3f);
            }

            aliveEnemies.Add(newEnemy);
        }

        Debug.Log($"⚔️ Wave {currentWave} spawned {enemyCount} enemies!");
        spawningWave = false;
    }

    bool AllEnemiesCleared()
    {
        aliveEnemies.RemoveAll(e => e == null);
        return aliveEnemies.Count == 0 && !spawningWave;
    }

    void UpdateWaveUI()
    {
        if (waveText != null)
            waveText.text = $"WAVE {currentWave}";
    }

    public void RemoveEnemy(GameObject enemy)
    {
        if (aliveEnemies.Contains(enemy))
            aliveEnemies.Remove(enemy);
    }

    private void CheckPerfectWaveRewards()
    {
        PlayerHealth hp = FindFirstObjectByType<PlayerHealth>();
        PlayerGunSystem gun = FindFirstObjectByType<PlayerGunSystem>();
        PlayerInventory inv = FindFirstObjectByType<PlayerInventory>();
        SlotMachine sm = FindFirstObjectByType<SlotMachine>();

        if (hp == null || gun == null || inv == null) return;

        // Not full health? No perfect bonus
        if (hp.currentHealth < hp.maxHealth)
            return;

        Debug.Log("🏆 PERFECT WAVE CLEAR!");

        // Player has no gun → give random gun
        if (gun.currentGun == null)
        {
            if (sm != null && sm.possibleGuns.Length > 0)
            {
                GunData reward = sm.possibleGuns[Random.Range(0, sm.possibleGuns.Length)];
                gun.GiveGun(reward);
                Debug.Log($"Given gun: {reward.gunName}");
            }
        }
        // Has gun but not full ammo → refill
        else if (gun.ammo < gun.currentGun.maxAmmo)
        {
            gun.ammo = gun.currentGun.maxAmmo;
            gun.ui?.UpdateAmmo(gun.ammo, gun.currentGun.maxAmmo);
        }
        // Everything full → give coins
        else
        {
            inv.AddCoins(10);
            Debug.Log("Bonus coins awarded!");
        }
    }
}
