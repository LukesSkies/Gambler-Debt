using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, OldIDamageable
{
    [Header("Stats")]
    public float maxHealth = 25f;
    public float damage = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public float detectionRange = 15f;
    [Range(10f, 180f)] public float fieldOfView = 90f;

    [Header("Behavior")]
    public float wanderRadius = 8f;
    public float rePathDelay = 1.5f;
    public float avoidanceRadius = 2f;
    public float loseSightDelay = 4f;

    [Header("Group Coordination")]
    public float alertRadius = 10f;
    public float alertChance = 0.75f;
    public float alertPersistence = 6f;

    [Header("UI")]
    public Slider healthBar;
    public Vector3 healthBarOffset = new Vector3(0, 2f, 0);

    [Header("References")]
    public GameObject model;

    private NavMeshAgent agent;
    private Transform player;
    private EnemyDropper dropper;
    private Camera mainCam;
    private WaveSpawner waveSpawner;

    private float currentHealth;
    private float lastAttackTime;
    private float wanderTimer;
    private float loseSightTimer;
    private float alertTimer;
    private bool playerVisible;
    private bool isDead;
    private bool isAlerted;

    [SerializeField] private string state;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        dropper = GetComponent<EnemyDropper>();
        mainCam = Camera.main;
        waveSpawner = FindAnyObjectByType<WaveSpawner>();

        currentHealth = maxHealth;
        UpdateHealthBar();

        agent.speed += Random.Range(-0.3f, 0.3f);
        agent.avoidancePriority = Random.Range(20, 80);
    }

    void Update()
    {
        if (isDead || player == null) return;

        UpdateHealthBarTransform();
        alertTimer = Mathf.Max(0, alertTimer - Time.deltaTime);

        if (!playerVisible && !isAlerted)
        {
            CheckForPlayer();
        }
        else
        {
            if (!PlayerInSight() && !isAlerted)
            {
                loseSightTimer += Time.deltaTime;
                if (loseSightTimer >= loseSightDelay)
                {
                    playerVisible = false;
                    loseSightTimer = 0;
                }
            }
            else
            {
                loseSightTimer = 0;
            }
        }

        if (playerVisible || isAlerted)
            HandleChaseAndStopAtRange();
        else
            WanderNearPlayer();

        AvoidOtherEnemies();
    }

    void HandleChaseAndStopAtRange()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            FacePlayerSmoothly();

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                AttackPlayer();
            }

            state = "Attacking";
        }
        else
        {
            if (agent.isStopped)
                agent.isStopped = false;

            agent.SetDestination(player.position);
            state = isAlerted ? "Alerted (Chasing)" : "Chasing";
        }
    }

    void WanderNearPlayer()
    {
        wanderTimer += Time.deltaTime;
        if (wanderTimer >= rePathDelay)
        {
            wanderTimer = 0f;
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += player.position;
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                agent.isStopped = false;
                state = "Wandering";
            }
        }
    }

    void FacePlayerSmoothly()
    {
        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);
    }

    void CheckForPlayer()
    {
        if (player == null) return;

        Vector3 dirToPlayer = player.position - transform.position;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        if (dirToPlayer.magnitude <= detectionRange && angle <= fieldOfView / 2f)
        {
            if (PlayerInSight())
            {
                playerVisible = true;
                AlertNearbyEnemies();
                state = "Player Spotted";
            }
        }
    }

    bool PlayerInSight()
    {
        Vector3 dirToPlayer = player.position - transform.position;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer.normalized, out RaycastHit hit, detectionRange))
        {
            return hit.collider.CompareTag("Player");
        }
        return false;
    }

    void AttackPlayer()
    {
        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage);
        }
    }

    public void AlertNearbyEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, alertRadius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<EnemyAI>(out var ally) && ally != this && !ally.isDead)
            {
                if (Random.value <= alertChance)
                    ally.BecomeAlerted();
            }
        }
    }

    public void BecomeAlerted()
    {
        isAlerted = true;
        alertTimer = alertPersistence;
        playerVisible = true;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        UpdateHealthBar();

        AlertNearbyEnemies();
        BecomeAlerted();

        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        ComboSystem.Instance?.RegisterKill();

        dropper?.DropLoot();
        waveSpawner?.RemoveEnemy(this.gameObject);

        if (agent)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        SetCollidersEnabled(false);
        if (model != null) model.SetActive(false);

        Destroy(gameObject, 0.3f);
    }

    void AvoidOtherEnemies()
    {
        Collider[] others = Physics.OverlapSphere(transform.position, avoidanceRadius);
        Vector3 push = Vector3.zero;
        int count = 0;

        foreach (var c in others)
        {
            if (c != null && c.gameObject != gameObject && c.CompareTag("Enemy"))
            {
                Vector3 away = transform.position - c.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.001f)
                {
                    push += away.normalized;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            push /= count;
            agent.Move(push.normalized * 0.015f);
        }
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.value = Mathf.Clamp01(currentHealth / maxHealth);
    }

    void UpdateHealthBarTransform()
    {
        if (healthBar != null && mainCam != null)
        {
            healthBar.transform.position = transform.position + healthBarOffset;
            healthBar.transform.rotation = Quaternion.LookRotation(healthBar.transform.position - mainCam.transform.position);
        }
    }

    void SetCollidersEnabled(bool enabled)
    {
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = enabled;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }
}
