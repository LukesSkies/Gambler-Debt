using Biostart.Impact;
using TMPro;
using UnityEngine;

public class RaycastGun : MonoBehaviour
{
    [Header("Gun Settings")]
    public GunSettings GunSettings;
    [SerializeField] private Vector3 _startingPos;
    [SerializeField] private Vector3 _aimingPos;
    [SerializeField] private float _timeToAim;
    public bool Aiming;
    public bool ReloadQueued;

    [Header("Graphics")]
    public GameObject _muzzleFlash;
    [SerializeField] private GameObject _metalSparks;
    [SerializeField] private GameObject _bulletHole;
    [SerializeField] private float _bulletHoleDelay;

    [Header("Debug")]
    [SerializeField] private bool _allowInvoke = true;
    [SerializeField] private bool _allowRaycastShown = true;
    private Vector3 _debugDirection;
    public bool InputShooting;
    public bool CanAim;
    [SerializeField] private int _bulletsLeft, _bulletsShot, _reserveAmmo;
    [SerializeField] private bool _shooting, _readyToShoot, _reloading;

    private TextMeshProUGUI _gunName;
    private TextMeshProUGUI _gunAmmo;

    private Camera _mainCam;
    private Camera _weaponCamera;
    private Transform _attackPoint;
    private Transform _weaponHolder;
    private Transform _pointAdditionParent;

    private float _aimTimer = 0f;

    private Collider _playerCollider;

    private GunRecoil _gunRecoil;
    private PlayerMove _playerMove;
    private PlayerCurrentGun _playerCurrentGun;
    private PlayerCamera _playerCamera;

    private Points _points;

    private ParticleSystem _bulletCasings;

    private Animator _gunAnimator;

    private void Awake()
    {
        _mainCam = Camera.main;
        _weaponCamera = _mainCam.transform.parent.transform.Find("GunCamera").GetComponent<Camera>();

        _attackPoint = transform.Find("WeaponMesh").transform.Find("AttackPoint");

        _playerCollider = transform.root.Find("PlayerMesh").GetComponent<Collider>();

        _gunRecoil = transform.root.Find("CameraHolder").transform.Find("CameraRecoil").GetComponent<GunRecoil>();
        _playerMove = transform.root.GetComponent<PlayerMove>();
        _playerCurrentGun = transform.root.GetComponent<PlayerCurrentGun>();

        _weaponHolder = transform.parent;

        _gunName = GameObject.Find("HUD").transform.Find("GunStats").transform.Find("GunName").GetComponent<TextMeshProUGUI>();
        _gunAmmo = GameObject.Find("HUD").transform.Find("GunStats").transform.Find("GunAmmo").GetComponent<TextMeshProUGUI>();

        _points = transform.root.GetComponent<Points>();

        _bulletCasings = transform.Find("WeaponMesh").transform.Find("CasingSpawnPoint").transform.Find("BulletCasings").GetComponent<ParticleSystem>();
        _gunAnimator = transform.Find("WeaponMesh").GetComponent<Animator>();

        _pointAdditionParent = GameObject.Find("HUD").transform.Find("Points").transform.Find("Player0").transform.Find("PointAdditionParent");
    }

    void Start()
    {
        _weaponHolder.localPosition = _startingPos;

        _bulletsLeft = GunSettings.MagazineSize;
        _readyToShoot = true;
        CanAim = true;

        _reserveAmmo = GunSettings.ReserveAmmo;

        _gunRecoil.GunSettings = GunSettings;

        UpdateHUD();
    }

    // Update is called once per frame
    void Update()
    {
        PlayerInput();
        GunDebug();
    }

    private void PlayerInput()
    {
        _shooting = InputShooting;
        InputShooting = false;

        if (ReloadQueued)
        {
            if (_bulletsLeft < GunSettings.MagazineSize && !_reloading && _reserveAmmo > 0)
            {
                _reloading = true;
                CanAim = false;
                _gunAnimator.SetTrigger("Reloading");
            }

            ReloadQueued = false;
        }

        if (!_reloading && _bulletsLeft <= 0 && _reserveAmmo > 0)
        {
            _reloading = true;
            CanAim = false;
            _gunAnimator.SetTrigger("Reloading");
        }

        if (CanAim && Aiming)
        {
            _aimTimer += Time.deltaTime;
            _gunRecoil.IsAiming = true;
        }
        else
        {
            _aimTimer -= Time.deltaTime;
            _gunRecoil.IsAiming = false;
        }

        _aimTimer = Mathf.Clamp(_aimTimer, 0f, _timeToAim);

        UpdateAimPositionAndFOV();

        if (_playerMove.IsSliding)
        {
            CanAim = false;
        }
        else
        {
            CanAim = true;
        }

        if (_readyToShoot && _shooting && !_reloading && _bulletsLeft > 0 && _playerCurrentGun.CanShoot)
        {
            _bulletsShot = 0;

            Shoot();
        }
    }

    private void Shoot()
    {
        _readyToShoot = false;

        for (int i = 0; i < GunSettings.BulletsPerTap; i++)
        {
            FireRaycast();
        }

        _gunRecoil.RecoilFire();
        _bulletCasings.Emit(1);

        if (_muzzleFlash != null)
            _muzzleFlash.GetComponent<ParticleSystem>().Play();

        _bulletsLeft--;
        _bulletsShot++;

        UpdateHUD();

        if (_allowInvoke)
        {
            Invoke("ResetShot", GunSettings.TimeBetweenShooting);
            _allowInvoke = false;
        }
    }

    private void FireRaycast()
    {
        float x = Random.Range(-GunSettings.Spread, GunSettings.Spread);
        float y = Random.Range(-GunSettings.Spread, GunSettings.Spread);

        // Fire from camera center with spread applied
        Vector3 direction = _mainCam.transform.forward
            + _mainCam.transform.right * x
            + _mainCam.transform.up * y;

        _debugDirection = direction;

        direction.Normalize();

        RaycastHit[] hits = Physics.RaycastAll(_mainCam.transform.position, direction, GunSettings.GunRange, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.name == "Barrier")
                continue;

            Debug.Log(hit.transform.name);

            Vector3 hitPoint = hit.point;
            Vector3 hitNormal = hit.normal;

            Transform hitTransform = hit.transform;

            if (hit.transform.gameObject.layer == 11)
            {
                ImpactEffect impact = hit.collider.GetComponent<ImpactEffect>();
                if (impact != null)
                    impact.SpawnBloodEffect(hitPoint, hitNormal);

                _points.Money += GameManager.Instance.HitPoints;
                Instantiate(GameManager.Instance.PointsAdditionText, _pointAdditionParent);
            }
            else
            {
                if (_metalSparks != null)
                {
                    GameObject sparks = Instantiate(_metalSparks, hitPoint, Quaternion.identity);
                    sparks.transform.forward = hitNormal;
                }

                if (_bulletHole != null)
                {
                    GameObject bulletHole = Instantiate(_bulletHole, hitPoint, Quaternion.identity);
                    bulletHole.transform.rotation = Quaternion.LookRotation(-hitNormal)
                        * Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                    Destroy(bulletHole, _bulletHoleDelay);
                }
            }

            // Damage
            EnemyTakeDamage enemyTakeDamage = hit.collider.GetComponent<EnemyTakeDamage>();
            if (enemyTakeDamage != null)
            {
                float damageMultiplier = enemyTakeDamage.BulletBodyType switch
                {
                    EnemyTakeDamage.BodyType.Head => GunSettings.HeadDamageMultiplier,
                    EnemyTakeDamage.BodyType.Chest => GunSettings.ChestDamageMultiplier,
                    EnemyTakeDamage.BodyType.Abdomen => GunSettings.AbdomenDamageMultiplier,
                    _ => 1f
                };
                enemyTakeDamage.EnemyHealth.TakeDamage(GunSettings.BulletDamage, damageMultiplier);
            }

            break;
        }
    }

    private void GunDebug()
    {
        if (_allowRaycastShown)
        {
            Vector3 direction = _mainCam.transform.forward;
            Debug.DrawRay(_mainCam.transform.position, direction * GunSettings.GunRange, Color.blue);
        }
    }

    private void ResetShot()
    {
        _readyToShoot = true;
        _allowInvoke = true;
    }

    public void Reload()
    {
        int bulletsNeeded = GunSettings.MagazineSize - _bulletsLeft;
        int bulletsToReload = Mathf.Min(bulletsNeeded, _reserveAmmo);

        _bulletsLeft += bulletsToReload;
        _reserveAmmo -= bulletsToReload;

        _reloading = false;

        if (!_playerMove.IsSliding)
        {
            CanAim = true;
        }

        UpdateHUD();
    }

    private void UpdateAimPositionAndFOV()
    {
        float time = _aimTimer / _timeToAim;
        _weaponHolder.localPosition = Vector3.Lerp(_startingPos, _aimingPos, time);
    }

    public void UpdateHUD()
    {
        _gunName.text = GunSettings.GunName;
        _gunAmmo.text = _bulletsLeft + "/" + _reserveAmmo;
    }
}
