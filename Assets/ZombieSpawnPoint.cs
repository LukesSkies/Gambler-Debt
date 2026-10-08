using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieSpawnPoint : MonoBehaviour
{
    private Transform _spawnPointParent;

    [SerializeField] private List<Transform> _spawnPoints;

    private void Awake()
    {
        _spawnPointParent = transform.Find("SpawnPoints");
    }

    void Start()
    {
        foreach(Transform child in _spawnPointParent)
        {
            _spawnPoints.Add(child);
        }
    }

    public bool CheckIfEnemyCanSpawn()
    {
        foreach (Transform child in _spawnPoints)
        {
            // Found a spawn point with an enemy not spawning
            if (child.childCount == 0)
            {
                return true;
            }
        }

        //Enemy spawning in all points or list is empty
        return false;
    }

    public void SpawnZombie(bool canRun = false, bool jogging = false)
    {
        foreach (Transform child in _spawnPoints)
        {
            if (child.childCount == 0)
            {
                GameObject zombie = Instantiate(GameManager.Instance.Zombie, child);
                NavMeshAgent zombieNavMesh = zombie.GetComponent<NavMeshAgent>();
                EnemyAI enemyAI = zombie.GetComponent<EnemyAI>();
                EnemyHealth enemyHealth = zombie.GetComponent<EnemyHealth>();

                //Jogging
                if(canRun && jogging)
                {
                    zombieNavMesh.speed = RoundSystem.Instance.JoggingZombieSpeed;
                    enemyAI.CanJog = true;
                    enemyHealth.EnemyType = EnemyHealth.TypeOfEnemy.Jogging;
                }

                //Running Zombie
                if (canRun)
                {
                    zombieNavMesh.speed = RoundSystem.Instance.RunningZombieSpeed;
                    enemyAI.CanRun = true;
                    enemyHealth.EnemyType = EnemyHealth.TypeOfEnemy.Running;
                }

                //Walking Zombie
                else
                {
                    zombieNavMesh.speed = RoundSystem.Instance.WalkingZombieSpeed;
                    enemyAI.CanRun = false;
                    enemyHealth.EnemyType = EnemyHealth.TypeOfEnemy.Walking;
                }
                break;
            }
        }
    }
}
