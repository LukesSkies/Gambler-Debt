using System.Xml.Serialization;
using UnityEngine;
public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private float _sensX;
    [SerializeField] private float _sensY;
    [SerializeField] private float _slideRot;
    [SerializeField] private float _slideRotSpeed;

    private Rigidbody _rb;
    private PlayerMove _pm;

    private float _xRot;
    private float _yRot;

    private float _zRot;

    private void Awake()
    {
        _rb = GetComponentInParent<Rigidbody>();
        _pm = GetComponentInParent<PlayerMove>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        //Get Input
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * _sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * _sensY;

        _yRot += mouseX;

        _xRot -= mouseY;
        _xRot = Mathf.Clamp(_xRot, -90, 90);

        HandleCameraTilt();

        transform.rotation = Quaternion.Euler(_xRot, _yRot, _zRot);

        if (!_pm.IsSliding)
        {
            _rb.MoveRotation(Quaternion.Euler(0, _yRot, 0));
        }
    }

    private void HandleCameraTilt()
    {
        float targetZRot = 0f;

        if (_pm.IsSliding)
        {
            targetZRot = _slideRot;
        }

        _zRot = Mathf.Lerp(_zRot, targetZRot, Time.deltaTime * _slideRotSpeed);
    }
}
