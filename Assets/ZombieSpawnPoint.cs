using System.Collections.Generic;
using UnityEngine;

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

    public void SpawnZombie()
    {
        foreach (Transform child in _spawnPoints)
        {
            if (child.childCount == 0)
            {
                Instantiate(GameManager.Instance.Zombie, child);
                break;
            }
        }
    }
}
