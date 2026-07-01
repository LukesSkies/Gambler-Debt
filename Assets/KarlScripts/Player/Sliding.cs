using UnityEngine;
using UnityEngine.Rendering;

public class Sliding : MonoBehaviour
{
    private Rigidbody _rb;
    private PlayerMove _pm;

    [Header("Sliding")]
    [SerializeField] private float _maxSlideTime;
    [SerializeField] private float _slideForce;
    [SerializeField] private float _slideYScale;
    private float _startYScale;
    private float _slideTimer;

    [Header("Keybinds")]
    [SerializeField] private KeyCode _slideKey = KeyCode.C;
    private float _horizontalInput;
    private float _verticalInput;

    private bool _sliding;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _pm = GetComponent<PlayerMove>();

        _startYScale = transform.localScale.y;
    }

    void Update()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        if(Input.GetKeyDown(_slideKey) && _verticalInput > 0)
        {
            StartSlide();
        }
    }

    private void FixedUpdate()
    {
        if (_sliding)
        {
            SlidingMovement();
        }
    }

    private void StartSlide()
    {
        _sliding = true;
        transform.localScale = new Vector3(transform.localScale.x, _slideYScale, transform.localScale.z);
        _rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        _slideTimer = _maxSlideTime;
    }

    private void SlidingMovement()
    {
        Vector3 inputDir = transform.forward * _verticalInput + transform.right * _horizontalInput;

        if (!_pm.OnSlope() || _rb.linearVelocity.y > -0.1f)
        {
            _rb.AddForce(inputDir.normalized * _slideForce * 10f, ForceMode.Force);

            _slideTimer -= Time.deltaTime;
        }
        else
        {
            _rb.AddForce(_pm.GetSlopeMoveDir(inputDir) * _slideForce * 10f, ForceMode.Force);
        }

        if (_slideTimer <= 0)
        {
            StopSlide();
        }
    }

    private void StopSlide()
    {
        _sliding = false;
        transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);
        _rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
    }
}
