using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    public float Health { get; set; }
    [SerializeField] private float _health;
    private bool _healthSet;
    private EnemyAI _enemyAI;
    public enum TypeOfEnemy
    {
        Walking,
        Running,
        Jogging
    }

    public TypeOfEnemy EnemyType;

    private void Awake()
    {
        _enemyAI = GetComponent<EnemyAI>();
        _health = GameManager.Instance.GetZombieHealth(GameManager.Instance.Round);
    }

    private void Start()
    {
        _health = GameManager.Instance.GetZombieHealth(GameManager.Instance.Round);
    }

    private void Update()
    {
        if(!_healthSet && _health == 0)
        {
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
        ZombieSpawner.Instance.CurrentZombieCount--;
        ZombieSpawner.Instance.RoundZombieCount--;

        switch (EnemyType)
        {
            case TypeOfEnemy.Walking:
                ZombieSpawner.Instance.CurrentWalkingZombieCount--;
                break;
            case TypeOfEnemy.Running:
                ZombieSpawner.Instance.CurrentRunningZombieCount--;
                break;
            case TypeOfEnemy.Jogging:
                ZombieSpawner.Instance.CurrentJoggingZombieCount--;
                break;
        }

        _enemyAI.EnemySpawner.ZombiesInSpawner.Remove(transform);
        Destroy(gameObject);
    }
}
