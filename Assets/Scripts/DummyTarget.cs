using UnityEngine;
using UnityEngine.UI;

public class DummyTarget : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float respawnDelay = 3f;

    [Header("References")]
    public GameObject model;
    public Slider healthBar;  
    public Transform respawnPoint;    
    private EnemyDropper dropper;      

    private float currentHealth;
    private bool isDead;
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
        currentHealth = maxHealth;
        UpdateHealthBar();

        dropper = GetComponent<EnemyDropper>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        UpdateHealthBar();

        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        isDead = true;

        dropper?.DropLoot();

        if (model != null) model.SetActive(false);
        SetCollidersEnabled(false);

        Invoke(nameof(Respawn), respawnDelay);
    }

    void Respawn()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();

        Vector3 respawnPos = respawnPoint != null ? respawnPoint.position : startPosition;
        transform.position = respawnPos;

        if (model != null) model.SetActive(true);
        SetCollidersEnabled(true);
        isDead = false;
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.value = Mathf.Clamp01(currentHealth / maxHealth);
    }

    void SetCollidersEnabled(bool enabled)
    {
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = enabled;
    }
}
