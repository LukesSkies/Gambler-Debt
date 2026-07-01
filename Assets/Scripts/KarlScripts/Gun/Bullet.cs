using UnityEngine;
using Biostart.Impact;

public class Bullet : MonoBehaviour
{
    private Rigidbody _rb;

    [SerializeField] private GameObject _metalSparks;
    [SerializeField] private GameObject _bulletHole;
    [SerializeField] private LayerMask _enemyLayer;

    [SerializeField][Range(0,1)] private float _bounciness;
    [SerializeField] private bool _useGravity;

    public float BulletDamage;
    public float HeadDamageMultiplier;
    public float ChestDamageMultiplier;
    public float AbdomenDamageMultiplier;
    [SerializeField] private float _explosionRange;

    [SerializeField] private int _maxCollisions;
    [SerializeField] private float _maxLifetime;
    [SerializeField] private bool _explodeOnTouch = true;
    [SerializeField] private float _bulletHoleDelay;

    private int _collisions;
    private PhysicsMaterial _physicsMaterial;
    public Vector3 ExplosionNormal;

    [HideInInspector]public Points PointsScript;

    private Transform _pointAdditionParent;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _pointAdditionParent = GameObject.Find("HUD").transform.Find("Points").transform.Find("Player0").transform.Find("PointAdditionParent");
    }

    private void Start()
    {
        _physicsMaterial = new PhysicsMaterial();
        _physicsMaterial.bounciness = _bounciness;
        _physicsMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
        _physicsMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;

        foreach(GameObject zombieBarriers in GameManager.Instance.ZombieSpawnBarriers)
        {
            Physics.IgnoreCollision(zombieBarriers.GetComponent<Collider>(), GetComponent<Collider>());
        }

        GetComponent<SphereCollider>().material = _physicsMaterial;

        _rb.useGravity = _useGravity;
    }

    private void Update()
    {
        _maxLifetime -= Time.deltaTime;
        if(_maxLifetime <= 0) Explode(0);
    }

    private void Explode(int layer, Vector3? explosionPosition = null)
    {
        Vector3 spawnPosition = explosionPosition ?? transform.position;

        if (_metalSparks != null && layer != 11)
        {
            GameObject explosion = Instantiate(_metalSparks, spawnPosition, Quaternion.identity);
            explosion.transform.forward = ExplosionNormal;
        }

        if(_bulletHole != null && layer != 11)
        {
            GameObject bulletHole = Instantiate(_bulletHole, spawnPosition, Quaternion.identity);
            bulletHole.transform.rotation = Quaternion.LookRotation(-ExplosionNormal)
            * Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            Destroy(bulletHole, _bulletHoleDelay);
        }

        Collider[] enemies = Physics.OverlapSphere(spawnPosition, _explosionRange, _enemyLayer);

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i].GetComponent<EnemyTakeDamage>())
            {
                if (enemies[i].GetComponent<EnemyTakeDamage>().BulletBodyType == EnemyTakeDamage.BodyType.Head)
                {
                    enemies[i].GetComponent<EnemyTakeDamage>().EnemyHealth.TakeDamage(BulletDamage, HeadDamageMultiplier);
                }
                else if (enemies[i].GetComponent<EnemyTakeDamage>().BulletBodyType == EnemyTakeDamage.BodyType.Chest)
                {
                    enemies[i].GetComponent<EnemyTakeDamage>().EnemyHealth.TakeDamage(BulletDamage, ChestDamageMultiplier);
                }
                else
                {
                    enemies[i].GetComponent<EnemyTakeDamage>().EnemyHealth.TakeDamage(BulletDamage, AbdomenDamageMultiplier);
                }
            }
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        _collisions++;
        ContactPoint contact = collision.contacts[0];
        ExplosionNormal = contact.normal;

        // Walk up hierarchy to find the enemy layer
        Transform hitTransform = collision.transform;
        while (hitTransform != null)
        {
            if (hitTransform.gameObject.layer == 11)
            {
                ImpactEffect impact = hitTransform.GetComponent<ImpactEffect>();
                if (impact != null)
                    impact.SpawnBloodEffect(transform.position, contact.normal);

                PointsScript.Money += GameManager.Instance.HitPoints;
                Instantiate(GameManager.Instance.PointsAdditionText, _pointAdditionParent);
                break;
            }
            hitTransform = hitTransform.parent;
        }

        if (_explodeOnTouch)
            Explode(collision.gameObject.layer, contact.point);
        else if (_collisions >= _maxCollisions)
            Explode(collision.gameObject.layer, contact.point);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRange);
    }
}
