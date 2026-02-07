using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Bullet")]
    [SerializeField] private GameObject _bullet;
    [SerializeField] private float _shootForce;
    [SerializeField] private float _upwardForce;

    [Header("Gun Settings")]
    [SerializeField] private float _timeBetweenShooting;
    [SerializeField] private float _spread;
    [SerializeField] private float _reloadTime;
    [SerializeField] private float _timeBetweenShots;
    [SerializeField] private int _magazineSize;
    [SerializeField] private int _bulletsPerTap;
    [SerializeField] private bool _allowButtonHold;

    [Header("Graphics")]
    public GameObject _muzzleFlash;

    [Header("Debug")]
    [SerializeField] private bool _allowInvoke = true;

    private int _bulletsLeft, _bulletsShot;
    private bool _shooting, _readyToShoot, _reloading;

    private Camera _mainCam;
    private Transform _attackPoint;

    private Collider _playerCollider;

    private GunRecoil _gunRecoil;

    private void Awake()
    {
        _mainCam = Camera.main;
        _attackPoint = transform.Find("AttackPoint");
        _playerCollider = transform.root.Find("PlayerMesh").GetComponent<Collider>();
        _gunRecoil = transform.root.Find("CameraHolder").GetChild(0).GetComponent<GunRecoil>();
    }

    void Start()
    {
        _bulletsLeft = _magazineSize;
        _readyToShoot = true;
    }

    // Update is called once per frame
    void Update()
    {
        PlayerInput();
    }

    private void PlayerInput()
    {
        if (_allowButtonHold)
        {
            _shooting = Input.GetMouseButton(0);
        }
        else
        {
            _shooting = Input.GetMouseButtonDown(0);
        }

        if(Input.GetKeyDown(KeyCode.R) && _bulletsLeft < _magazineSize && !_reloading)
        {
            Reload();
        }

        if(_readyToShoot && _shooting && _reloading && _bulletsLeft <= 0)
        {
            Reload();
        }

        if(_readyToShoot && _shooting && !_reloading && _bulletsLeft > 0)
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

        float x = Random.Range(-_spread, _spread);
        float y = Random.Range(-_spread, _spread);

        // Add spread relative to camera orientation
        targetPoint += _mainCam.transform.right * x;
        targetPoint += _mainCam.transform.up * y;

        Vector3 directionWithSpread = (targetPoint - _attackPoint.position).normalized;

        GameObject currentBullet = Instantiate(_bullet, _attackPoint.position, Quaternion.identity);
        currentBullet.transform.forward = directionWithSpread.normalized;
        currentBullet.GetComponent<Rigidbody>().AddForce(directionWithSpread.normalized * _shootForce, ForceMode.Impulse);
        currentBullet.GetComponent<Rigidbody>().AddForce(_mainCam.transform.up * _upwardForce, ForceMode.Impulse);
        Physics.IgnoreCollision(_playerCollider, currentBullet.GetComponent<Collider>());

        _gunRecoil.RecoilFire();

        if(_muzzleFlash != null)
        {
            _muzzleFlash.GetComponent<ParticleSystem>().Play();
        }

        _bulletsLeft--;
        _bulletsShot++;

        if (_allowInvoke)
        {
            Invoke("ResetShot", _timeBetweenShooting);
            _allowInvoke = false;
        }

        //Shotgun pellets
        if(_bulletsShot < _bulletsPerTap && _bulletsLeft > 0)
        {
            Invoke("ResetShot", _timeBetweenShots);
        }
    }

    private void ResetShot()
    {
        _readyToShoot = true;
        _allowInvoke = true;
    }

    private void Reload()
    {
        _reloading = true;
        Invoke("ReloadFinish", _reloadTime);
    }

    private void ReloadFinish()
    {
        _bulletsLeft = _magazineSize;
        _reloading = false;
    }
}
