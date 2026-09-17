using UnityEngine;
using System.Collections;

public class PlayerHP : MonoBehaviour, IDamageable
{
    private GameplayMenus _gameplayMenus;

    public float Health { get; set; }
    [SerializeField] private float _debugCurrentHealth;
    public float MaxHealth;
    public float WaitUntilRegen;

    public int AmountOfDowns;

    [SerializeField] private Animation _redFlash;

    private Coroutine _regenHealth;

    [Header("Debug")]
    [SerializeField] private bool WaitForHealthRegen;
    public bool DeathToggle;

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

        if (Input.GetKeyDown(KeyCode.H))
        {
            Health = 0;
        }

        if (Health <= 0 && !DeathToggle)
        {
            DeathToggle = true;
            Health = 0;
            _redFlash.Stop();
            _redFlash.Rewind();
            Death();
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
        AmountOfDowns++;
        _gameplayMenus.ReviveQTE.SetActive(true);
        _gameplayMenus.ReviveQTE.GetComponent<ReviveQTE>().ResetMinigame();
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