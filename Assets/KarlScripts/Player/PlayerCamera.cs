using UnityEngine;
public class PlayerCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public float MouseSensitivity;
    public float MouseAimSensitivity;
    public float ControllerSensitivity;
    public float ControllerAimSensitivity;
    public Vector2 PlayerLookVector;
    [SerializeField] private float _slideRot;
    [SerializeField] private float _slideRotSpeed;

    [Header("Headbob Settings")]
    [SerializeField] private bool _enableHeadbob;
    [SerializeField, Range(0, 1f)] private float _amplitude = 0.15f;
    [SerializeField, Range(0, 30)] private float _frequency = 10;
    [SerializeField, Range(0, 30)] private float _sprintFrequency = 20;

    [Header("FOV Settings")]
    [SerializeField] private float _timeToAim = 0.2f;
    public float FOVAimingChange = -20f;
    public float FOVSprintSpeedChange;
    [SerializeField] private float _fovChangeTime = 0.2f;

    [Header("FOV Debug")]
    public float DefaultFOV;
    public float DefaultGunFOV;
    public float SprintFOV;
    public float AimingFOV;
    public float AimingGunFOV;
    private Camera _mainCamera;
    private Camera _gunCamera;
    private float _aimTimer = 0f;
    private float _sprintTimer = 0f;
    private float _sprintTime;

    private Rigidbody _rb;
    private PlayerMove _playerMove;
    private PlayerCurrentGun _playerCurrentGun;
    private GunRecoil _gunRecoil;

    private float _xRot;
    private float _yRot;
    private float _zRot;

    private float _toggleSpeed = 3;
    private float _elapsedTime = 0;
    private Vector3 _startPos;

    private Vector2 _cameraLook;

    private void Awake()
    {
        _rb = GetComponentInParent<Rigidbody>();
        _playerMove = GetComponentInParent<PlayerMove>();
        _playerCurrentGun = GetComponentInParent<PlayerCurrentGun>();
        _gunRecoil = transform.Find("CameraRecoil").GetComponent<GunRecoil>();

        _startPos = transform.localPosition;

        _mainCamera = Camera.main;
        _gunCamera = _mainCamera.transform.parent.transform.Find("GunCamera").GetComponent<Camera>();

        DefaultFOV = PlayerPrefs.GetFloat("PlayerFieldOfView", 80);
        DefaultGunFOV = PlayerPrefs.GetFloat("GunFieldOfView", 90);

        _mainCamera.fieldOfView = HFOVToVFOV(DefaultFOV);
        _gunCamera.fieldOfView = HFOVToVFOV(DefaultGunFOV);

        AimingFOV = DefaultFOV + FOVAimingChange;
        AimingGunFOV = DefaultGunFOV + FOVAimingChange;

        SprintFOV = DefaultFOV + FOVSprintSpeedChange;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (GameManager.Instance.PlayerDead)
        {
            _cameraLook = Vector2.zero;
        }

        else if (GameManager.Instance.Player0IsUsingKeyboardOrMouse)
        {
            _cameraLook = PlayerLookVector * Time.deltaTime * (_playerCurrentGun.CurrentGun.GetComponent<RaycastGun>().Aiming ? MouseAimSensitivity : MouseSensitivity);
        }

        else
        {
            _cameraLook = PlayerLookVector * Time.deltaTime * (_playerCurrentGun.CurrentGun.GetComponent<RaycastGun>().Aiming ? ControllerAimSensitivity : ControllerSensitivity);
        }

        _yRot += _cameraLook.x;

        _xRot -= _cameraLook.y;
        _xRot = Mathf.Clamp(_xRot, -90, 90);

        HandleCameraTilt();

        transform.rotation = Quaternion.Euler(_xRot, _yRot, _zRot);

        if (!_playerMove.IsSliding)
        {
            _rb.MoveRotation(Quaternion.Euler(0, _yRot, 0));
        }

        if (_enableHeadbob)
        {
            CheckMotion();
            ResetPos();
        }
    }

    private void Update()
    {
        FOVChecks();
        HandleFOVChange();
    }

    private void HandleCameraTilt()
    {
        float targetZRot = 0f;

        if (_playerMove.IsSliding)
        {
            targetZRot = _slideRot;
        }

        _zRot = Mathf.Lerp(_zRot, targetZRot, Time.deltaTime * _slideRotSpeed);
    }

    private void FOVChecks()
    {
        if (_playerCurrentGun.CurrentGun.GetComponent<RaycastGun>().CanAim && _playerCurrentGun.CurrentGun.GetComponent<RaycastGun>().Aiming)
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

        bool isSprintingOrSliding = _playerMove.IsSliding || _playerMove.State == PlayerMove.MovementState.sprinting || _playerMove.State == PlayerMove.MovementState.outSliding;

        if (isSprintingOrSliding)
        {
            _sprintTimer += Time.deltaTime;
        }
        else
        {
            _sprintTimer -= Time.deltaTime;
        }

        _sprintTimer = Mathf.Clamp(_sprintTimer, 0, _fovChangeTime);
    }

    private void HandleFOVChange()
    {
        float aimTime = _aimTimer / _timeToAim;
        _sprintTime = _sprintTimer / _fovChangeTime;

        float targetMainFOV = Mathf.Lerp(DefaultFOV, SprintFOV, _sprintTime);

        _mainCamera.fieldOfView = HFOVToVFOV(Mathf.Lerp(targetMainFOV, AimingFOV, aimTime));
        _gunCamera.fieldOfView = HFOVToVFOV(Mathf.Lerp(DefaultGunFOV, AimingGunFOV, aimTime));
    }

    private void CheckMotion()
    {
        float speed = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z).magnitude;

        if (speed < _toggleSpeed) return;
        if (_playerMove.IsSliding) return;
        if (!_playerMove.GroundCheck()) return;

        PlayMotion(FootstepMotion());
    }

    private void PlayMotion(Vector3 motion)
    {
        transform.localPosition += motion;
    }

    private void ResetPos()
    {
        if (transform.localPosition == _startPos) return;
        transform.localPosition = Vector3.Lerp(transform.localPosition, _startPos, 1 * Time.deltaTime);
    }

    private Vector3 FootstepMotion()
    {
        Vector3 pos = Vector3.zero;

        float time = Time.time - _elapsedTime;

        if(_playerMove.State == PlayerMove.MovementState.idle)
        {
            _elapsedTime = Time.time;
        }

        float currentFrequency = Mathf.Lerp(_frequency, _sprintFrequency, _sprintTime);

        pos.y += Mathf.Sin(time * currentFrequency) * _amplitude * Time.deltaTime;
        pos.x += Mathf.Cos(time * currentFrequency / 2) * _amplitude * 2 * Time.deltaTime;
        return pos;
    }

    public float VFOVToHFOV(float vfov)
    {
        float vfovRad = vfov * Mathf.Deg2Rad;
        float hfovRad = 2f * Mathf.Atan(Mathf.Tan(vfovRad / 2f) * _mainCamera.aspect);
        return hfovRad * Mathf.Rad2Deg;
    }

    public float HFOVToVFOV(float hfov)
    {
        float hfovRad = hfov * Mathf.Deg2Rad;
        float vfovRad = 2f * Mathf.Atan(Mathf.Tan(hfovRad / 2f) / _mainCamera.aspect);
        return vfovRad * Mathf.Rad2Deg;
    }
}
