using System.Collections.Generic;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    public bool StartZombieSpawer;
    public int CurrentZombieCount; //Active zombies in game
    public int RoundZombieCount; //Zombies left in the round
    public float ZombieSpawnerTimer;
    public List<GameObject> ActiveSpawners = new List<GameObject>();
    [SerializeField] private List<GameObject> _activeSpawnersCheck = new List<GameObject>();

    void Start()
    {
        ZombieSpawnerTimer = 0;
        _activeSpawnersCheck = ActiveSpawners;
    }

    void Update()
    {
        if (StartZombieSpawer)
        {
            ZombieSpawnerTimer += Time.deltaTime;
            if (ZombieSpawnerTimer >= GameManager.Instance.ZombieSpawnRate)
            {
                ZombieSpawnerTimer = 0;
                if (CurrentZombieCount == GameManager.Instance.MaxZombieCount[GameManager.Instance.PlayerCount - 1] ||
                    CurrentZombieCount == RoundZombieCount)
                {
                    Debug.Log("Spawner stopping. Current Zombie Count= " + CurrentZombieCount +" Max Zombies Count = " + GameManager.Instance.MaxZombieCount[GameManager.Instance.PlayerCount - 1] + ", Max Round Zombie Count = " +RoundZombieCount);
                    StartZombieSpawer = false;

                }
                else
                {
                    SpawnZombie();
                }
            }
        }

        //New Round
        if (RoundZombieCount <= 0)
        {
            RoundZombieCount = GameManager.Instance.GetZombieCount(GameManager.Instance.PlayerCount,
                GameManager.Instance.Round);
            CurrentZombieCount = 0;
        }
    }

    private void SpawnZombie()
    {
        while (_activeSpawnersCheck.Count > 0)
        {
            int randomSpawn = Random.Range(0, _activeSpawnersCheck.Count);
            Debug.Log(_activeSpawnersCheck[randomSpawn]);
            ZombieSpawnPoint spawnPoint = _activeSpawnersCheck[randomSpawn].GetComponent<ZombieSpawnPoint>();

            if (spawnPoint.CheckIfEnemyCanSpawn())
            {
                CurrentZombieCount++;
                spawnPoint.SpawnZombie();
                _activeSpawnersCheck = ActiveSpawners;
                return;
            }

            _activeSpawnersCheck.RemoveAt(randomSpawn);
        }
    }
}