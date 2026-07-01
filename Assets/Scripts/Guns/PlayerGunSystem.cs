using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerGunSystem : MonoBehaviour
{
    // GUN STATE
    public GunData currentGun;            // Currently equipped gun data
    public Transform gunHolder;           // Where the gun model is attached
    public Transform shootPoint;           // Bullet/trail origin
    public GameObject currentGunModel;     // Instantiated gun prefab
    public int ammo;                      // Current ammo count

    private bool canShoot = true;          // Fire-rate gate
    private bool isHoldingShoot = false;   // Full-auto input state

    // REFERENCES
    public GameObject arms;                // Arms mesh (hidden when gun equipped)
    private PlayerHealth playerHealth;     // Health reference (for gambling penalty)
    private PlayerMelee melee;             // Melee system (disabled when gun equipped)
    public PlayerUI ui;                    // HUD updates
    private PlayerControls controls;       // Input system
    public bool IsShooting => !canShoot;

    // GAMBLE SETTINGS
    public float healthPenalty = 20f;      // Health lost on failed reload gamble
    public float ammoRewardChance = 0.5f;  // Chance to refill ammo on gamble

    // VISUAL EFFECTS
    public TrailRenderer bulletTrailPrefab; // Bullet trail visual

    void Awake()
    {
        // Initialize input actions
        controls = new PlayerControls();

        controls.OldPlayer.Shoot.performed += _ => StartShooting();
        controls.OldPlayer.Shoot.canceled += _ => StopShooting();

        // Reload is repurposed as a risk/reward gamble mechanic
        controls.OldPlayer.Reload.performed += _ => GambleAmmo();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Start()
    {
        // Cache component references
        playerHealth = GetComponent<PlayerHealth>();
        melee = GetComponent<PlayerMelee>();
        ui = FindFirstObjectByType<PlayerUI>();
    }

    // EQUIP / UNEQUIP
    public void GiveGun(GunData newGun)
    {
        // Remove existing gun model
        if (currentGunModel)
            Destroy(currentGunModel);

        currentGun = newGun;
        ammo = newGun.maxAmmo;

        // Disable melee + arms visuals
        if (melee) melee.enabled = false;
        if (arms) arms.SetActive(false);

        // Spawn gun model
        if (gunHolder && newGun.gunPrefab)
        {
            currentGunModel = Instantiate(newGun.gunPrefab, gunHolder);
            currentGunModel.transform.localPosition = Vector3.zero;
            currentGunModel.transform.localRotation = Quaternion.identity;
        }

        // Update UI
        ui?.UpdateGunName(newGun.gunName);
        ui?.UpdateAmmo(ammo, newGun.maxAmmo);
    }

    // SHOOTING INPUT
    private void StartShooting()
    {
        if (currentGun == null) return;

        // Full-auto weapons
        if (currentGun.fullAuto)
        {
            isHoldingShoot = true;
            StartCoroutine(ShootLoop());
        }
        // Semi-auto weapons
        else if (canShoot)
        {
            StartCoroutine(FireRoutine());
        }
    }

    private void StopShooting() => isHoldingShoot = false;

    private IEnumerator ShootLoop()
    {
        // Continuous fire while button held
        while (isHoldingShoot && currentGun != null)
        {
            if (canShoot && ammo > 0)
                yield return FireRoutine();
            else
                yield return null;
        }
    }

    private IEnumerator FireRoutine()
    {
        canShoot = false;

        ammo--;
        ui?.UpdateAmmo(ammo, currentGun.maxAmmo);

        FireSmartWeapon();
        ApplyRecoil();

        // Fire-rate delay
        yield return new WaitForSeconds(currentGun.fireRate);
        canShoot = true;

        // Drop gun if out of ammo
        if (ammo <= 0)
            RemoveGun();
    }

    // FIRING LOGIC
    private void FireSmartWeapon()
    {
        if (Camera.main == null || currentGun == null)
            return;

        Vector3 camPos = Camera.main.transform.position;

        // Handle pellet-based weapons (shotguns, etc.)
        for (int i = 0; i < currentGun.pellets; i++)
        {
            Vector3 dir = GetSpreadDirection();
            Ray ray = new Ray(camPos, dir);

            // Direct hit
            if (Physics.Raycast(ray, out RaycastHit hit, currentGun.maxRange, currentGun.hitMask, QueryTriggerInteraction.Ignore))
            {
                ProcessHit(hit);
            }
            else
            {
                // Smart auto-aim fallback
                Transform target = SmartAutoAimTarget(dir, 8f, currentGun.maxEffectiveRange);
                if (target != null)
                {
                    ApplyDamage(target, Vector3.Distance(camPos, target.position));
                    SpawnTrailSafe(shootPoint.position, target.position);
                }
                else
                {
                    // Missed shot visual
                    SpawnTrailSafe(
                        shootPoint.position,
                        shootPoint.position + dir * currentGun.maxEffectiveRange
                    );
                }
            }
        }
    }

    private void ProcessHit(RaycastHit hit)
    {
        // Ignore slot machines for damage, only visuals
        if (hit.collider.GetComponentInParent<SlotMachine>())
        {
            SpawnTrailSafe(shootPoint.position, hit.point);
            return;
        }

        // Try to damage hit target
        IDamageable dmg =
            hit.collider.GetComponent<IDamageable>() ??
            hit.collider.GetComponentInParent<IDamageable>();

        if (dmg != null)
        {
            float dist = Vector3.Distance(Camera.main.transform.position, hit.point);
            float baseDamage = CalculateDamageWithFalloff(currentGun.damage, dist);

            float comboMult = ComboSystem.Instance != null
                ? ComboSystem.Instance.GetDamageMultiplier()
                : 1f;

            float finalDamage = baseDamage * comboMult;

            if (finalDamage > 0)
                dmg.TakeDamage(finalDamage);
        }

        SpawnTrailSafe(shootPoint.position, hit.point);
    }

    private void ApplyDamage(Transform target, float dist)
    {
        IDamageable dmg =
            target.GetComponent<IDamageable>() ??
            target.GetComponentInParent<IDamageable>();

        if (dmg == null) return;

        float baseDamage = CalculateDamageWithFalloff(currentGun.damage, dist);

        float comboMult = ComboSystem.Instance != null
            ? ComboSystem.Instance.GetDamageMultiplier()
            : 1f;

        float finalDamage = baseDamage * comboMult;

        if (finalDamage > 0)
            dmg.TakeDamage(finalDamage);
    }

    // AIM / SPREAD
    private Vector3 GetSpreadDirection()
    {
        // Apply random spread relative to camera
        Vector3 dir = Camera.main.transform.forward;
        dir += Camera.main.transform.right * Random.Range(-currentGun.spread, currentGun.spread);
        dir += Camera.main.transform.up * Random.Range(-currentGun.spread, currentGun.spread);
        return dir.normalized;
    }

    private Transform SmartAutoAimTarget(Vector3 dir, float angle, float maxDist)
    {
        // Finds best enemy within cone in front of camera
        Vector3 camPos = Camera.main.transform.position;

        Collider[] hits = Physics.OverlapSphere(
            camPos + dir * (maxDist * 0.5f),
            maxDist * 0.35f
        );

        Transform best = null;
        float bestScore = Mathf.Infinity;

        foreach (var h in hits)
        {
            if (!h.CompareTag("Enemy")) continue;

            Vector3 to = h.transform.position - camPos;
            float dist = to.magnitude;
            float ang = Vector3.Angle(dir, to);

            if (dist > maxDist || ang > angle) continue;

            // Prefer closer + more centered targets
            float score = ang + dist * 0.01f;
            if (score < bestScore)
            {
                bestScore = score;
                best = h.transform;
            }
        }

        return best;
    }

    // DAMAGE FALLOFF
    private float CalculateDamageWithFalloff(float baseDamage, float dist)
    {
        if (dist <= currentGun.falloffStartRange)
            return baseDamage;

        if (dist >= currentGun.maxEffectiveRange)
            return 0f;

        float t =
            (dist - currentGun.falloffStartRange) /
            (currentGun.maxEffectiveRange - currentGun.falloffStartRange);

        return Mathf.Lerp(baseDamage, 0f, t);
    }

    // VISUALS
    private void SpawnTrailSafe(Vector3 start, Vector3 end)
    {
        if (!bulletTrailPrefab || !shootPoint) return;

        var tr = Instantiate(bulletTrailPrefab, start, Quaternion.identity);
        StartCoroutine(PlayBulletTrail(tr, start, end));
    }

    private IEnumerator PlayBulletTrail(TrailRenderer tr, Vector3 start, Vector3 end)
    {
        float t = 0f;
        while (t < 0.05f)
        {
            tr.transform.position = Vector3.Lerp(start, end, t / 0.05f);
            t += Time.deltaTime;
            yield return null;
        }

        tr.transform.position = end;
        Destroy(tr.gameObject, tr.time);
    }

    private void ApplyRecoil()
    {
        // Small randomized camera kickback
        Camera.main.transform.localRotation *= Quaternion.Euler(
            -currentGun.recoilAmount,
            Random.Range(
                -currentGun.recoilAmount * 0.5f,
                currentGun.recoilAmount * 0.5f
            ),
            0f
        );
    }

    // GAMBLE / RELOAD
    public void GambleAmmo()
    {
        if (currentGun == null) return;

        // Successful gamble: refill ammo
        if (Random.value < ammoRewardChance)
        {
            ammo = currentGun.maxAmmo;
            ui?.UpdateAmmo(ammo, currentGun.maxAmmo);
        }
        // Failed gamble: lose health and gun
        else
        {
            playerHealth?.TakeDamage(healthPenalty);
            RemoveGun();
        }
    }

    private void RemoveGun()
    {
        if (currentGunModel)
            Destroy(currentGunModel);

        currentGun = null;

        // Restore melee + arms
        melee.enabled = true;
        arms.SetActive(true);

        ui?.UpdateGunName("Fists");
        ui?.UpdateAmmo(0, 0);
    }
}
