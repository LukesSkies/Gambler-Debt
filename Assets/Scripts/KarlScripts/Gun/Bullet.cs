using System.Xml.Serialization;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Rigidbody _rb;

    [SerializeField] private GameObject _metalSparks;
    [SerializeField] private LayerMask _enemyLayer;

    [SerializeField][Range(0,1)] private float _bounciness;
    [SerializeField] private bool _useGravity;

    public float BulletDamage;
    [SerializeField] private float _explosionRange;

    [SerializeField] private int _maxCollisions;
    [SerializeField] private float _maxLifetime;
    [SerializeField] private bool _explodeOnTouch = true;

    private int _collisions;
    private PhysicsMaterial _physicsMaterial;
    public Vector3 ExplosionNormal;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _physicsMaterial = new PhysicsMaterial();
        _physicsMaterial.bounciness = _bounciness;
        _physicsMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
        _physicsMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;

        GetComponent<SphereCollider>().material = _physicsMaterial;

        _rb.useGravity = _useGravity;
    }

    private void Update()
    {
        _maxLifetime -= Time.deltaTime;
        if(_maxLifetime <= 0) Explode();
    }

    private void Explode(Vector3? explosionPosition = null)
    {
        Vector3 spawnPosition = explosionPosition ?? transform.position;

        if (_metalSparks != null)
        {
            GameObject explosion = Instantiate(_metalSparks, spawnPosition, Quaternion.identity);
            explosion.transform.forward = ExplosionNormal;
        }

        Collider[] enemies = Physics.OverlapSphere(spawnPosition, _explosionRange, _enemyLayer);

        for (int i = 0; i < enemies.Length; i++)
        {
            //enemies[i].GetComponent<EnemyHealth>().TakeHealth(BulletDamage);
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        _collisions++;

        ContactPoint contact = collision.contacts[0];
        ExplosionNormal = contact.normal;

        if (_explodeOnTouch)
        {
            Explode(contact.point);
        }
        else if (_collisions >= _maxCollisions)
        {
            Explode(contact.point); // Pass the contact point
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRange);
    }
}
