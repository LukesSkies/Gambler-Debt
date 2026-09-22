using System.Collections.Generic;
using UnityEngine;

public class EnemyInsideSpawnerCheck : MonoBehaviour
{
    public List<Transform> _zombiesInSpawner;

    private void Start()
    {
        _zombiesInSpawner = transform.root.GetComponent<EnemySpawner>().ZombiesInSpawner;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "EnemyBase")
        {
            _zombiesInSpawner.Add(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "EnemyBase")
        {
            _zombiesInSpawner.Remove(other.transform);
        }
    }
}