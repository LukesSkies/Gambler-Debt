using UnityEngine;
using System.Collections;

public class PlayerHP : MonoBehaviour, IDamageable
{
    private GameplayMenus _gameplayMenus;

    public float Health { get; set; }
    [SerializeField] private float _debugCurrentHealth;
    public float MaxHealth;
    public float WaitUntilRegen;

    [SerializeField] private Animation _redFlash;

    private Coroutine _regenHealth;

    [Header("Debug")]
    [SerializeField] private bool WaitForHealthRegen;

    void Start()
    {
        Health = GameManager.Instance.PlayerHealth;
        MaxHealth = GameManager.Instance.PlayerHealth;
        _gameplayMenus = GameObject.Find("Menus&QTE").GetComponent<GameplayMenus>();
    }

    private void Update()
    {
        if (_debugCurrentHealth != Health)
        {
            _debugCurrentHealth = Health;
        }

        if(WaitForHealthRegen && _regenHealth == null)
        {
            _regenHealth = StartCoroutine(RegenHealth());
        }
    }

    public void TakeDamage(float amount, float damageMultiplier = 1)
    {
        Health -= amount;
        WaitForHealthRegen = true;

        if (_regenHealth != null)
        {
            StopCoroutine(_regenHealth);
            _regenHealth = null;
        }

        if (Health <= 0)
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
        _gameplayMenus.DeathMenu.SetActive(true);
        GameManager.Instance.PlayerDead = true;
        _gameplayMenus.PlayerCurrentGun.CurrentGun.SetActive(false);
    }

    private IEnumerator RegenHealth()
    {
        yield return new WaitForSeconds(WaitUntilRegen);
        WaitForHealthRegen = false;
        Health = MaxHealth;
    }
}