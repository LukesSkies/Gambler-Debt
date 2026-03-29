using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public List<GameObject> Barriers  = new List<GameObject>();

    void Start()
    {
        foreach(Transform child in transform.Find("Barriers"))
        {
            Barriers.Add(child.gameObject);
        }
    }
}
