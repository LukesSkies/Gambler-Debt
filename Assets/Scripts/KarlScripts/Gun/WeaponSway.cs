using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("Weapon Sway")]
    [SerializeField] private float _swayAmount = 1f;
    [SerializeField] private float _swaySmoothing = 1;
    [SerializeField] private bool _swayInverted;
    [SerializeField] private float _swayResetSmoothing = 1;
    [SerializeField] private float _swayClampX = 1;
    [SerializeField] private float _swayClampY = 1;

    [Header("Weapon Movement Sway")]
    [SerializeField] private float _movementSwayX;
    [SerializeField] private float _movementSwayY;
    [SerializeField] private float _movementSwaySmoothing = 1;

    private Vector3 _newWeaponRotation;
    private Vector3 _newWeaponRotationVelocity;

    private Vector3 _targetWeaponRotation;
    private Vector3 _targetWeaponRotationVelocity;

    private Vector3 _newWeaponMovementRotation;
    private Vector3 _newWeaponMovementRotationVelocity;

    private Vector3 _targetWeaponMovementRotation;
    private Vector3 _targetWeaponMovementRotationVelocity;

    private void Start()
    {
        _newWeaponRotation = transform.localRotation.eulerAngles;
    }

    void Update()
    {
        //Weapon Sway 
        _targetWeaponRotation.y += _swayAmount * Input.GetAxis("Mouse X") * Time.deltaTime;
        _targetWeaponRotation.x += _swayAmount * (_swayInverted ? -Input.GetAxis("Mouse Y") : Input.GetAxis("Mouse Y")) * Time.deltaTime;

        _targetWeaponRotation.x = Mathf.Clamp(_targetWeaponRotation.x, -_swayClampX, _swayClampX);
        _targetWeaponRotation.y = Mathf.Clamp(_targetWeaponRotation.y, -_swayClampY, _swayClampY);
        _targetWeaponRotation.z = _targetWeaponRotation.y;

        _targetWeaponRotation = Vector3.SmoothDamp(_targetWeaponRotation, Vector3.zero, ref _targetWeaponRotationVelocity, _swayResetSmoothing);
        _newWeaponRotation = Vector3.SmoothDamp(_newWeaponRotation, _targetWeaponRotation, ref _newWeaponRotationVelocity, _swaySmoothing);

        //Weapon Movement Sway 
        _targetWeaponMovementRotation.z = _movementSwayX * Input.GetAxis("Horizontal") * Time.deltaTime;
        _targetWeaponMovementRotation.x = _movementSwayY * Input.GetAxis("Vertical") * Time.deltaTime;

        _targetWeaponMovementRotation = Vector3.SmoothDamp(_targetWeaponMovementRotation, Vector3.zero, ref _targetWeaponMovementRotationVelocity, _movementSwaySmoothing);
        _newWeaponMovementRotation = Vector3.SmoothDamp(_newWeaponMovementRotation, _targetWeaponMovementRotation, ref _newWeaponMovementRotationVelocity, _movementSwaySmoothing);

        transform.localRotation = Quaternion.Euler(_newWeaponRotation + _newWeaponMovementRotation);
    }
}
