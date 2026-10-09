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

    private enum TypeOfZombies
    {
        Walker,
        Runner
    }

    public bool StartZombieSpawer;
    public int CurrentZombieCount; //Active zombies in game
    public int CurrentWalkingZombieCount; //Active walking zombies in game
    public int CurrentJoggingZombieCount; //Active jogging zombies in game
    public int CurrentRunningZombieCount; //Active fast zombies in game
    public int RoundZombieCount; //Zombies left in the round
    public int RoundZombieWalkingCount; //Walking Zombies left in the round
    public int RoundZombieJoggingCount; //Jogging Zombies left in the round
    public int RoundZombieRunningCount; //Running Zombies left in the round
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

                //Late rounds if there are just running zombies
                if (RoundZombieRunningCount == RoundZombieCount)
                {
                    spawnPoint.SpawnZombie(true);
                    CurrentRunningZombieCount++;
                }

                //Early round where there isnt any jogging zombies
                else if (RoundZombieJoggingCount == 0)
                {
                    spawnPoint.SpawnZombie();
                    CurrentWalkingZombieCount++;
                }

                else
                {
                    int randomZombieType = Random.Range(0, 3);
                    switch (randomZombieType)
                    {
                        //Running Zombie
                        case 0:
                            //Spawn Zombie that isnt Running
                            if (CurrentRunningZombieCount == RoundZombieRunningCount)
                            {
                                //Spawn jogging zombie if there are any to spawn in
                                if(CurrentWalkingZombieCount == RoundZombieWalkingCount && RoundZombieJoggingCount != 0)
                                {
                                    spawnPoint.SpawnZombie(true, true);
                                    CurrentJoggingZombieCount++;
                                }
                                //Spawn walking zombie
                                else
                                {
                                    spawnPoint.SpawnZombie();
                                    CurrentWalkingZombieCount++;
                                }
                            }

                            //Spawn running zombie
                            else
                            {
                                spawnPoint.SpawnZombie(true);
                                CurrentRunningZombieCount++;
                            }
                            break;
                        //Walking Zombie
                        case 1:
                            //Spawn Zombie that isnt walking
                            if (CurrentWalkingZombieCount == RoundZombieWalkingCount)
                            {
                                //Spawn jogging zombie if there are any to spawn in
                                if (CurrentRunningZombieCount == RoundZombieRunningCount && CurrentJoggingZombieCount != RoundZombieJoggingCount)
                                {
                                    spawnPoint.SpawnZombie(true, true);
                                    CurrentJoggingZombieCount++;
                                }
                                //Spawn sprinting zombie
                                else
                                {
                                    spawnPoint.SpawnZombie(true);
                                    CurrentRunningZombieCount++;
                                }
                            }

                            //Spawn walking zombie
                            else
                            {
                                spawnPoint.SpawnZombie();
                                CurrentWalkingZombieCount++;
                            }
                            break;

                        //Jogging Zombie
                        case 2:
                            //Spawn Zombie that isnt jogging
                            if (CurrentJoggingZombieCount == RoundZombieJoggingCount)
                            {
                                //Spawn running zombie if there are any to spawn in
                                if (CurrentWalkingZombieCount == RoundZombieWalkingCount && CurrentRunningZombieCount != RoundZombieRunningCount)
                                {
                                    spawnPoint.SpawnZombie(true);
                                    CurrentRunningZombieCount++;
                                }
                                //Spawn walking zombie
                                else
                                {
                                    spawnPoint.SpawnZombie();
                                    CurrentWalkingZombieCount++;
                                }
                            }

                            //Spawn jogging zombie
                            else
                            {
                                spawnPoint.SpawnZombie(true, true);
                                CurrentJoggingZombieCount++;
                            }
                            break;
                    }
                }
                _activeSpawnersCheck = ActiveSpawners;
                return;
            }

            _activeSpawnersCheck.RemoveAt(randomSpawn);
        }
    }
}