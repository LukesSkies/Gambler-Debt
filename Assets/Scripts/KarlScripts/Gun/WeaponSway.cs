using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("Weapon Mouse Sway")]
    [SerializeField] private float _swayAmount = 1f;
    [SerializeField] private float _swayAimAmount;
    [SerializeField] private float _swaySmoothing = 1;
    [SerializeField] private bool _swayInverted;
    [SerializeField] private float _swayResetSmoothing = 1;
    [SerializeField] private float _swayClampX = 1;
    [SerializeField] private float _swayClampY = 1;
    [SerializeField] private float _swayAimClampX;
    [SerializeField] private float _swayAimClampY;

    [Header("Weapon Movement Sway")]
    [SerializeField] private float _movementSwayX;
    [SerializeField] private float _movementSwayY;
    [SerializeField] private float _aimMovementSwayX;
    [SerializeField] private float _aimMovementSwayY;
    [SerializeField] private float _movementSwaySmoothing = 1;

    [Header("Weapon Movement Bobbing")]
    [SerializeField] private float _bobBlendSpeed = 0.15f;
    [SerializeField] private float _bobPositionSmoothing = 0.08f;
    [SerializeField] private float _speedCurve;
    private float _curveSin { get => Mathf.Sin(_speedCurve); }
    private float _curveCos { get => Mathf.Cos(_speedCurve); }
    [SerializeField] private Vector3 _travelLimit = Vector3.one * 0.025f;
    [SerializeField] private Vector3 _bobLimit = Vector3.one * 0.01f;
    private Vector3 _bobPosition;
    private float _bobWeight;
    private Vector3 _smoothedBobPosition;
    private Vector3 _smoothedBobVelocity;

    [Header("Weapon Breathing Sway")]
    [SerializeField] private float _swayAmountA = 1;
    [SerializeField] private float _swayAmountB = 2;
    [SerializeField] private float _swayScale = 600;
    [SerializeField] private float _swayLerpSpeed = 14;
    private float _swayTime;
    private Vector3 _swayPosition;
    private Vector3 _targetPos;

    private Vector3 _newWeaponRotation;
    private Vector3 _newWeaponRotationVelocity;

    private Vector3 _targetWeaponRotation;
    private Vector3 _targetWeaponRotationVelocity;

    private Vector3 _newWeaponMovementRotation;
    private Vector3 _newWeaponMovementRotationVelocity;

    private Vector3 _targetWeaponMovementRotation;
    private Vector3 _targetWeaponMovementRotationVelocity;

    private Gun _gun;
    private RaycastGun _raycastGun;
    private PlayerMove _playerMove;
    private Rigidbody _rb;
    private bool _raycastGunBool;

    private void Awake()
    {
        if (GetComponent<RaycastGun>())
        {
            _raycastGun = GetComponent<RaycastGun>();
            _raycastGunBool = true;
        }
        else
        {
            _gun = GetComponent<Gun>();
            _raycastGunBool = false;
        }

        _playerMove = transform.root.GetComponent<PlayerMove>();
        _rb = transform.root.GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _newWeaponRotation = transform.localRotation.eulerAngles;
    }

    void Update()
    {
        transform.localPosition = WeaponBreathingSway() + WeaponMovementBobbing();

        transform.localRotation = Quaternion.Euler(WeaponAimSway() + WeaponMovementSway());
    }

    private Vector3 WeaponBreathingSway()
    {
        if (!_raycastGunBool)
        {
            _targetPos = LissajousCurve(_swayTime, _gun.Aiming ? _swayAmountA / 2 : _swayAmountA, _swayAmountB) / _swayScale;
        }
        else
        {
            _targetPos = LissajousCurve(_swayTime, _raycastGun.Aiming ? _swayAmountA / 2 : _swayAmountA, _swayAmountB) / _swayScale;
        }

        _swayPosition = Vector3.Lerp(_swayPosition, _targetPos, Time.smoothDeltaTime * _swayLerpSpeed);
        _swayTime += Time.deltaTime;

        if(_swayTime > 6.3f)
        {
            _swayTime = 0;
        }

        return _swayPosition;
    }

    private Vector3 WeaponMovementBobbing()
    {
        if (!_raycastGunBool)
        {
            _bobWeight = _gun.Aiming ? 0.2f : 1;
        }
        else
        {
            _bobWeight = _raycastGun.Aiming ? 0.2f : 1;
        }

        _speedCurve += Time.deltaTime * (_playerMove.GroundCheck() ? _rb.linearVelocity.magnitude : 1f) + 0.01f;
        _bobPosition.x = (_curveCos * _bobLimit.x * (_playerMove.GroundCheck() ? 1 : 0f)) - _travelLimit.x;
        _bobPosition.y = (_curveSin * _bobLimit.y) - (_rb.linearVelocity.y * _travelLimit.y);
        _bobPosition.z = -_travelLimit.z;

        _smoothedBobPosition = Vector3.SmoothDamp(_smoothedBobPosition, _bobPosition, ref _smoothedBobVelocity, _bobPositionSmoothing);
        return _smoothedBobPosition * _bobWeight;
    }

    private Vector3 WeaponAimSway()
    {
        if (!_raycastGunBool)
        {
            if (_gun.Aiming)
            {
                _targetWeaponRotation.y += _swayAimAmount * Input.GetAxis("Mouse X") * Time.deltaTime;
                _targetWeaponRotation.x += _swayAimAmount * (_swayInverted ? -Input.GetAxis("Mouse Y") : Input.GetAxis("Mouse Y")) * Time.deltaTime;

                _targetWeaponRotation.x = Mathf.Clamp(_targetWeaponRotation.x, -_swayAimClampX, _swayAimClampX);
                _targetWeaponRotation.y = Mathf.Clamp(_targetWeaponRotation.y, -_swayAimClampY, _swayAimClampY);
            }
            else
            {
                _targetWeaponRotation.y += _swayAmount * Input.GetAxis("Mouse X") * Time.deltaTime;
                _targetWeaponRotation.x += _swayAmount * (_swayInverted ? -Input.GetAxis("Mouse Y") : Input.GetAxis("Mouse Y")) * Time.deltaTime;

                _targetWeaponRotation.x = Mathf.Clamp(_targetWeaponRotation.x, -_swayClampX, _swayClampX);
                _targetWeaponRotation.y = Mathf.Clamp(_targetWeaponRotation.y, -_swayClampY, _swayClampY);
            }
        }
        else
        {
            if (_raycastGun.Aiming)
            {
                _targetWeaponRotation.y += _swayAimAmount * Input.GetAxis("Mouse X") * Time.deltaTime;
                _targetWeaponRotation.x += _swayAimAmount * (_swayInverted ? -Input.GetAxis("Mouse Y") : Input.GetAxis("Mouse Y")) * Time.deltaTime;

                _targetWeaponRotation.x = Mathf.Clamp(_targetWeaponRotation.x, -_swayAimClampX, _swayAimClampX);
                _targetWeaponRotation.y = Mathf.Clamp(_targetWeaponRotation.y, -_swayAimClampY, _swayAimClampY);
            }
            else
            {
                _targetWeaponRotation.y += _swayAmount * Input.GetAxis("Mouse X") * Time.deltaTime;
                _targetWeaponRotation.x += _swayAmount * (_swayInverted ? -Input.GetAxis("Mouse Y") : Input.GetAxis("Mouse Y")) * Time.deltaTime;

                _targetWeaponRotation.x = Mathf.Clamp(_targetWeaponRotation.x, -_swayClampX, _swayClampX);
                _targetWeaponRotation.y = Mathf.Clamp(_targetWeaponRotation.y, -_swayClampY, _swayClampY);
            }
        }

        _targetWeaponRotation.z = _targetWeaponRotation.y;

        _targetWeaponRotation = Vector3.SmoothDamp(_targetWeaponRotation, Vector3.zero, ref _targetWeaponRotationVelocity, _swayResetSmoothing);
        _newWeaponRotation = Vector3.SmoothDamp(_newWeaponRotation, _targetWeaponRotation, ref _newWeaponRotationVelocity, _swaySmoothing);

        return _newWeaponRotation;
    }

    private Vector3 WeaponMovementSway()
    {
        if (!_raycastGunBool)
        {
            if (_gun.Aiming)
            {
                _targetWeaponMovementRotation.z = _aimMovementSwayX * Input.GetAxisRaw("Horizontal") * Time.deltaTime;
                _targetWeaponMovementRotation.x = _aimMovementSwayY * Input.GetAxisRaw("Vertical") * Time.deltaTime;
            }
            else
            {
                _targetWeaponMovementRotation.z = _movementSwayX * Input.GetAxisRaw("Horizontal") * Time.deltaTime;
                _targetWeaponMovementRotation.x = _movementSwayY * Input.GetAxisRaw("Vertical") * Time.deltaTime;
            }
        }
        else
        {
            if (_raycastGun.Aiming)
            {
                _targetWeaponMovementRotation.z = _aimMovementSwayX * Input.GetAxisRaw("Horizontal") * Time.deltaTime;
                _targetWeaponMovementRotation.x = _aimMovementSwayY * Input.GetAxisRaw("Vertical") * Time.deltaTime;
            }
            else
            {
                _targetWeaponMovementRotation.z = _movementSwayX * Input.GetAxisRaw("Horizontal") * Time.deltaTime;
                _targetWeaponMovementRotation.x = _movementSwayY * Input.GetAxisRaw("Vertical") * Time.deltaTime;
            }
        }

        _targetWeaponMovementRotation = Vector3.SmoothDamp(_targetWeaponMovementRotation, Vector3.zero, ref _targetWeaponMovementRotationVelocity, _movementSwaySmoothing);
        _newWeaponMovementRotation = Vector3.SmoothDamp(_newWeaponMovementRotation, _targetWeaponMovementRotation, ref _newWeaponMovementRotationVelocity, _movementSwaySmoothing);

        return _newWeaponMovementRotation;
    }

    //Calculating Breathing Vector
    private Vector3 LissajousCurve(float Time, float A, float B)
    {
        return new Vector3(Mathf.Sin(Time), A * Mathf.Sin(B * Time + Mathf.PI));
    }
}
