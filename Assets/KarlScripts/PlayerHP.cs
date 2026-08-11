using UnityEngine;

public class PlayerHP : MonoBehaviour, IDamageable
{
    private GameplayMenus _gameplayMenus;

    public float Health { get; set; }
    [SerializeField] private float _debugCurrentHealth;
    public float MaxHealth;
    [SerializeField] private Animation _redFlash;

    void Start()
    {
        Health = GameManager.Instance.PlayerHealth;
        MaxHealth = GameManager.Instance.PlayerHealth;
        _gameplayMenus = GameObject.Find("Menus").GetComponent<GameplayMenus>();
    }

    private void Update()
    {
        if (_debugCurrentHealth != Health)
        {
            _debugCurrentHealth = Health;
        }
    }

    public void TakeDamage(float amount, float damageMultiplier = 1)
    {
        Health -= amount;
        if(Health <= 0)
        {
            Health = 0;
            _redFlash.Stop();
            _redFlash.Rewind();
            Death();
        }
    }

    public void Heal(float amount)
    {
        Health += amount;
        if (Health >= MaxHealth)
        {
            Health = MaxHealth;
        }
    }

    private void Death()
    {
        GameManager.Instance.Paused = true;
        GameManager.Instance.EndGame = true;
        _gameplayMenus.DeathMenu.SetActive(true);
        _gameplayMenus.PlayerCurrentGun.CanShoot = false;
    }
}