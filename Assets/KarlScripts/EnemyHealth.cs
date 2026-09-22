using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    public float Health { get; set; }
    [SerializeField] private float _health;
    private bool _healthSet;

    private void Update()
    {
        if(!_healthSet && _health == 0)
        {
            _health = GameManager.Instance.ZombieHealth;
            Health = _health;
        }
        if(_health > 0 && !_healthSet)
        {
            _healthSet = true;
        }
    }

    private void OnValidate()
    {
        Health = _health;
    }

    public void TakeDamage(float amount, float damageMultiplier)
    {
        _health -= amount * damageMultiplier;
        if (_health <= 0)
        {
            _health = 0;
            Death();
        }
    }

    public void Heal(float amount)
    {
        return;
    }

    private void Death()
    {
        Destroy(gameObject);
    }
}
