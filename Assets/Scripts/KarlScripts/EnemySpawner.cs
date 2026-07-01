using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject Barrier;
    private Collider _barrierCollider;
    private Material _barrierMaterial;
    public List<Transform> ZombiesInSpawner;
    public bool BarriersEnabled;

    public int BarrierHealth = 5;
    private int _maxBarrierHealth;

    void Awake()
    {
        Barrier = transform.Find("Mesh").transform.Find("Barrier").gameObject;
        _barrierCollider = Barrier.GetComponent<Collider>();
        _barrierMaterial = Barrier.GetComponent<MeshRenderer>().material;
        GameManager.Instance.ZombieSpawnBarriers.Add(Barrier);
    }

    void Start()
    {
        _maxBarrierHealth = 5;
        BarriersEnabled = true;
    }

    public void ZombieBarrierHit()
    {
        if (!BarriersEnabled) return;

        BarrierHealth -= 1;

        if (BarrierHealth == 0)
        {
            _barrierMaterial.SetFloat("_Transparency", BarrierHealth);

            foreach (Transform zombie in ZombiesInSpawner)
            {
                zombie.GetComponent<EnemyStateMachine>().DisableBarrierCollider(_barrierCollider);
            }
            BarriersEnabled = false;
            return;
        }

        float materialTransparency = (float)BarrierHealth / _maxBarrierHealth;

        _barrierMaterial.SetFloat("_Transparency", materialTransparency);
    }

    public void ZombieBarrierGain()
    {
        if (BarriersEnabled) return;

        BarrierHealth += 1;

        if (BarrierHealth == 5)
        {
            _barrierMaterial.SetFloat("_Transparency", BarrierHealth);

            foreach (Transform zombie in ZombiesInSpawner)
            {
                zombie.GetComponent<EnemyStateMachine>().EnableBarrierCollider(_barrierCollider);
            }
            BarriersEnabled = true;
            return;
        }

        float materialTransparency = (float)BarrierHealth / _maxBarrierHealth;

        _barrierMaterial.SetFloat("_Transparency", materialTransparency);
    }
}