using System.Collections.Generic;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    private static ZombieSpawner _instance;
    public static ZombieSpawner Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("Need Zombie Spawner in the scene :(");
            }
            return _instance;
        }
    }

    public bool StartZombieSpawer;
    public int CurrentZombieCount; //Active zombies in game
    public int CurrentSlowZombieCount; //Active slow zombies in game
    public int CurrentFastZombieCount; //Active fast zombies in game
    public int RoundZombieCount; //Zombies left in the round
    public int RoundZombieSlowCount; //Slow Zombies left in the round
    public int RoundZombiesFastCount; //Fast Zombies left in the round
    public float ZombieSpawnerTimer;
    public List<GameObject> ActiveSpawners = new List<GameObject>();
    [SerializeField] private List<GameObject> _activeSpawnersCheck = new List<GameObject>();

    private void Awake()
    {
        _instance = this;
    }

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
            if (ZombieSpawnerTimer >= RoundSystem.Instance.ZombieSpawnRate)
            {
                ZombieSpawnerTimer = 0;
                if (CurrentZombieCount == RoundSystem.Instance.MaxZombieCount[GameManager.Instance.PlayerCount - 1] ||
                    CurrentZombieCount == RoundZombieCount)
                {
                    Debug.Log("Spawner stopping. Current Zombie Count= " + CurrentZombieCount +" Max Zombies Count = " + RoundSystem.Instance.MaxZombieCount[GameManager.Instance.PlayerCount - 1] + ", Max Round Zombie Count = " +RoundZombieCount);
                    StartZombieSpawer = false;

                }
                else
                {
                    SpawnZombie();
                }
            }
        }

        if(CurrentZombieCount < RoundZombieCount)
        {
            StartZombieSpawer = true;
        }

        //New Round
        if (RoundZombieCount <= 0)
        {
            RoundSystem.Instance.NewRound();
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