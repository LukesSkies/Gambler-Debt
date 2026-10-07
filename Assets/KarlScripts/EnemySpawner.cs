using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject Barrier;
    public GameObject MovePoint;
    private Collider _barrierCollider;
    private Material _barrierMaterial;
    public List<Transform> ZombiesInSpawner;
    public bool BarriersEnabled;
    public bool OpenSpawn;

    public int BarrierHealth = 5;
    private int _maxBarrierHealth;

    void Awake()
    {
        Barrier = transform.Find("Mesh").transform.Find("Barrier").gameObject;
        MovePoint = transform.Find("Mesh").transform.Find("MovePoint").gameObject;
        _barrierCollider = Barrier.GetComponent<Collider>();
        _barrierMaterial = Barrier.GetComponent<MeshRenderer>().material;
    }

    void Start()
    {
        GameManager.Instance.ZombieSpawnBarriers.Add(Barrier);
        _maxBarrierHealth = 5;
        BarriersEnabled = true;
    }

    private void Update()
    {
        ZombieBarrierHealth();
    }

    public void ZombieBarrierHit()
    {
        if (!BarriersEnabled) return;

        BarrierHealth -= 1;

        if(BarrierHealth <= 0)
        {
            BarrierHealth = 0;
        }
    }

    public void ZombieBarrierHealth()
    {
        if (BarrierHealth == 0)
        {
            _barrierMaterial.SetFloat("_Transparency", 0);

            foreach (Transform zombie in ZombiesInSpawner)
            {
                zombie.GetComponent<EnemyStateMachine>().DisableBarrierCollider(_barrierCollider);
            }
            BarriersEnabled = false;
            return;
        }
        else if (BarrierHealth == 5)
        {
            _barrierMaterial.SetFloat("_Transparency", 1);

            if(ZombiesInSpawner.Count > 0)
            {
                foreach (Transform zombie in ZombiesInSpawner)
                {
                    zombie.GetComponent<EnemyStateMachine>().EnableBarrierCollider(_barrierCollider);
                }
            }
            BarriersEnabled = true;
            return;
        }
        else
        {
            float materialTransparency = (float)BarrierHealth / _maxBarrierHealth;

            _barrierMaterial.SetFloat("_Transparency", materialTransparency);
        }
    }

    public void ZombieBarrierGain()
    {
        if (BarriersEnabled) return;

        BarrierHealth += 1;
    }
}