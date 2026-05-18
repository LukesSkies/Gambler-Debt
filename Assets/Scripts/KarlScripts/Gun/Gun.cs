using TMPro;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Gun Settings")]
    [SerializeField] private GunSettings _gunSettings;
    [SerializeField] private Vector3 _startingPos;
    [SerializeField] private Vector3 _aimingPos;
    [SerializeField] private float _timeToAim;
    public bool Aiming;
    public bool UpdateHUD;

    [Header("Graphics")]
    public GameObject _muzzleFlash;

    [Header("Debug")]
    [SerializeField] private bool _allowInvoke = true;

    private int _bulletsLeft, _bulletsShot, _reserveAmmo;
    private bool _shooting, _readyToShoot, _reloading, _canAim;

    private TextMeshProUGUI _gunName;
    private TextMeshProUGUI _gunAmmo;

    private Camera _mainCam;
    private Camera _weaponCamera;
    private Transform _attackPoint;
    private Transform _weaponHolder;

    private float _defaultFOV;
    private float _defaultGunFOV;
    private float _aimingFOV;
    private float _aimingGunFOV;
    private float _aimTimer = 0f;

    private Collider _playerCollider;

    private GunRecoil _gunRecoil;
    private PlayerMove _playerMove;

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

        _weaponHolder = transform.parent;

        _gunName = GameObject.Find("HUD").transform.Find("GunStats").transform.Find("GunName").GetComponent<TextMeshProUGUI>();
        _gunAmmo = GameObject.Find("HUD").transform.Find("GunStats").transform.Find("GunAmmo").GetComponent<TextMeshProUGUI>();

        _points = transform.root.GetComponent<Points>();

        _bulletCasings = transform.Find("WeaponMesh").transform.Find("CasingSpawnPoint").transform.Find("BulletCasings").GetComponent<ParticleSystem>();
        _gunAnimator = transform.Find("WeaponMesh").GetComponent<Animator>();
    }

    void Start()
    {
        _defaultFOV = Camera.main.fieldOfView;
        _defaultGunFOV = _weaponCamera.fieldOfView;

        _aimingFOV = _defaultFOV - 20f;
        _aimingGunFOV = _defaultGunFOV - 20f;

        _weaponHolder.localPosition = _startingPos;

        _bulletsLeft = _gunSettings.MagazineSize;
        _readyToShoot = true;
        _canAim = true;

        _reserveAmmo = _gunSettings.ReserveAmmo;

        _gunRecoil.GunSettings = _gunSettings;

        UpdateHUD = true;
    }

    // Update is called once per frame
    void Update()
    {
        PlayerInput();
        HUD();
    }

    private void HUD()
    {
        if (UpdateHUD)
        {
            _gunName.text = _gunSettings.GunName;
            _gunAmmo.text = _bulletsLeft.ToString() + "/" + _reserveAmmo;
        }
    }

    private void PlayerInput()
    {
        if (_gunSettings.AllowButtonHold)
        {
            _shooting = Input.GetMouseButton(0);
        }
        else
        {
            _shooting = Input.GetMouseButtonDown(0);
        }

        Aiming = Input.GetMouseButton(1);

        if(Input.GetKeyDown(KeyCode.R) && _bulletsLeft < _gunSettings.MagazineSize && !_reloading && _reserveAmmo > 0)
        {
            _reloading = true;
            _canAim = false;
            _gunAnimator.SetTrigger("Reloading");
        }
        else if(_readyToShoot && _shooting && !_reloading && _bulletsLeft <= 0 && _reserveAmmo > 0)
        {
            _reloading = true;
            _canAim = false;
            _gunAnimator.SetTrigger("Reloading");
        }

        if (_canAim && Aiming)
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
            _canAim = false;
        }
        else
        {
            _canAim = true;
        }

        if (_readyToShoot && _shooting && !_reloading && _bulletsLeft > 0)
        {
            _bulletsShot = 0;

            Shoot();
        }
    }

    private void Shoot()
    {
        _readyToShoot = false;

        Ray ray = _mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        Vector3 targetPoint;
        if(Physics.Raycast(ray, out hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(75);
        }

        Vector3 directionWithoutSpread = targetPoint - _attackPoint.position;

        float x = Random.Range(-_gunSettings.Spread, _gunSettings.Spread);
        float y = Random.Range(-_gunSettings.Spread, _gunSettings.Spread);

        // Add spread relative to camera orientation
        targetPoint += _mainCam.transform.right * x;
        targetPoint += _mainCam.transform.up * y;

        Vector3 directionWithSpread = (targetPoint - _attackPoint.position).normalized;

        GameObject currentBullet = Instantiate(_gunSettings.Bullet, _attackPoint.position, Quaternion.identity);
        currentBullet.transform.forward = directionWithSpread.normalized;
        currentBullet.GetComponent<Rigidbody>().AddForce(directionWithSpread.normalized * _gunSettings.ShootForce, ForceMode.Impulse);
        currentBullet.GetComponent<Rigidbody>().AddForce(_mainCam.transform.up * _gunSettings.UpwardForce, ForceMode.Impulse);
        currentBullet.GetComponent<Bullet>().PointsScript = _points;
        Physics.IgnoreCollision(_playerCollider, currentBullet.GetComponent<Collider>());

        _gunRecoil.RecoilFire();

        _bulletCasings.Emit(1);

        if(_muzzleFlash != null)
        {
            _muzzleFlash.GetComponent<ParticleSystem>().Play();
        }

        _bulletsLeft--;
        _bulletsShot++;

        if (_allowInvoke)
        {
            Invoke("ResetShot", _gunSettings.TimeBetweenShooting);
            _allowInvoke = false;
        }

        //Shotgun pellets
        if(_bulletsShot < _gunSettings.BulletsPerTap && _bulletsLeft > 0)
        {
            Invoke("ResetShot", _gunSettings.TimeBetweenShots);
        }
    }

    private void ResetShot()
    {
        _readyToShoot = true;
        _allowInvoke = true;
    }

    public void Reload()
    {
        int bulletsNeeded = _gunSettings.MagazineSize - _bulletsLeft;
        int bulletsToReload = Mathf.Min(bulletsNeeded, _reserveAmmo);

        _bulletsLeft += bulletsToReload;
        _reserveAmmo -= bulletsToReload;

        _reloading = false;

        if (!_playerMove.IsSliding)
        {
            _canAim = true;
        }
    }

    private void UpdateAimPositionAndFOV()
    {
        float time = _aimTimer / _timeToAim;
        _weaponHolder.localPosition = Vector3.Lerp(_startingPos, _aimingPos, time);
        _mainCam.fieldOfView = Mathf.Lerp(_defaultFOV, _aimingFOV, time);
        _weaponCamera.fieldOfView = Mathf.Lerp(_defaultGunFOV, _aimingGunFOV, time);
    }

    private void OnDisable()
    {
        UpdateHUD = false;
    }

    private void OnEnable()
    {
        UpdateHUD = true;
    }
}
