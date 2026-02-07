using System.Collections;
using UnityEngine;


public class PlayerMove : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _walkSpeed;
    [SerializeField] private float _sprintSpeed;
    [SerializeField] private float _groundDrag;

    [Header("Jumping")]
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _jumpCooldown;
    [SerializeField] private float _airMultiplier;

    [Header("Crouching")]
    [SerializeField] private float _crouchSpeed;
    [SerializeField] private float _crouchHeight;
    [SerializeField] private float _crouchYScale;
    [SerializeField] private float _crouchLerpDuration;

    [Header("Sliding")]
    [SerializeField] private float _maxSlideTime;
    [SerializeField] private float _slideJumpForce;

    private float _slideTimer;
    private float _startHeight;
    private float _startYScale;
    private bool _readyToJump;
    private float _moveSpeed;

    [Header("Ground Check")]
    [SerializeField] private float _playerHeight;
    [SerializeField] private LayerMask _groundMask;
    private bool _grounded;

    [Header("Slope Handling")]
    [SerializeField] private float _maxSlopeAngle;
    private RaycastHit _slopeHit;
    private bool _exitingSlope;

    [Header("Keybinds")]
    [SerializeField] private KeyCode _jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode _sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode _crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode _slideKey = KeyCode.C;

    [Header("PlayerStates")]
    [SerializeField] private MovementState _state;

    private float _horizontalInput;
    private float _verticalInput;

    private Vector3 _moveDir;

    private Rigidbody _rb;

    private bool _isCrouching;
    [HideInInspector] public bool IsSliding;
    [HideInInspector] public bool JumpSlide;
    private bool _slideJump;
    private bool _slideJumpToggle;

    private CapsuleCollider _playerCollider;
    private Transform _playerMesh;

    private Vector2 _slideDir;

    private enum MovementState 
    {
        walking,
        sprinting,
        crouching,
        sliding,
        outSliding,
        air
    }

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
        _readyToJump = true;

        _playerMesh = transform.Find("PlayerMesh");
        _startYScale = _playerMesh.localScale.y;
    }

    void FixedUpdate()
    {
        if (IsSliding)
        {
            SlidingMovement();
        }
        else if(JumpSlide)
        {
            OutSide();

            if (_slideJumpToggle)
            {
                _slideJumpToggle = false;
                StartCoroutine(DelayJumping());
            }
        }
        else
        {
            MovePlayer();
        }
    }

    void Update()
    {
        PlayerInput();
        SpeedControl();
        StateHandler();

        if (GroundCheck())
        {
            _rb.linearDamping = _groundDrag;
        }
        else
        {
            _rb.linearDamping = 0;
        }
    }

    private void PlayerInput()
    {
        if (!IsSliding)
        {
            _horizontalInput = Input.GetAxisRaw("Horizontal");
        }
        _verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(_jumpKey) && _readyToJump && GroundCheck() && !_isCrouching)
        {
            _readyToJump = false;
            Jump();

            if (IsSliding)
            {
                IsSliding = false;
                _slideJump = true;
                JumpSlide = true;
                _slideJumpToggle = true;
                UnCrouch();
            }

            Invoke(nameof(ResetJump), _jumpCooldown);
        }

        if (Input.GetKeyDown(_slideKey) && _verticalInput > 0 && !_isCrouching && !IsSliding && _state == MovementState.sprinting)
        {
            _slideDir = new Vector2(_horizontalInput, _verticalInput);
            Crouch(true);
        }

        if (Input.GetKeyDown(_crouchKey) && !IsSliding)
        {
            Crouch(false);
        }

        if (Input.GetKeyUp(_crouchKey) && !IsSliding)
        {
            UnCrouch();
        }
    }

    private void StateHandler()
    {
        if (IsSliding)
        {
            _state = MovementState.sliding;
        }
        else if (JumpSlide)
        {
            _state = MovementState.outSliding;
            _moveSpeed = _sprintSpeed;
        }
        else if (_isCrouching)
        {
            _state = MovementState.crouching;
            _moveSpeed = _crouchSpeed;
        }
        else if(GroundCheck() && Input.GetKey(_sprintKey) && Input.GetKey(KeyCode.W))
        {
            _state = MovementState.sprinting;
            _moveSpeed = _sprintSpeed;
        }
        else if(GroundCheck())
        {
            _state = MovementState.walking;
            _moveSpeed = _walkSpeed;
        }
        else
        {
            _state = MovementState.air;
        }
    }

    private void MovePlayer()
    {
        _moveDir = transform.forward * _verticalInput + transform.right * _horizontalInput;

        if (OnSlope() && !_exitingSlope)
        {
            _rb.AddForce(GetSlopeMoveDir(_moveDir) * _moveSpeed * 20f, ForceMode.Force);

            if(_rb.linearVelocity.y > 0)
            {
                _rb.AddForce(Vector3.down * 40f, ForceMode.Force);
            }
        }
        else if (GroundCheck())
        {
            _rb.AddForce(_moveDir.normalized * _moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            _rb.AddForce(_moveDir.normalized * _moveSpeed * 10f * _airMultiplier, ForceMode.Force);
        }

        _rb.useGravity = !OnSlope();
    }

    private void OutSide()
    {
        Vector3 inputDir = transform.forward * _verticalInput + transform.right * _horizontalInput;

        _rb.AddForce(inputDir.normalized * _sprintSpeed * 10f, ForceMode.Force);

        _rb.useGravity = true;

        if (GroundCheck() && !_slideJump)
        {
            if (Input.GetKey(_slideKey) && _verticalInput > 0)
            {
                JumpSlide = false;
                _slideDir = new Vector2(_horizontalInput, _verticalInput);
                Crouch(true);
            }
            else
            {
                JumpSlide = false;
            }
        }
    }

    private void SpeedControl()
    {
        if (OnSlope() && !_exitingSlope)
        {
            if(_rb.linearVelocity.magnitude > _moveSpeed)
            {
                _rb.linearVelocity = _rb.linearVelocity.normalized * _moveSpeed;
            }
        }
        else
        {
            Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

            if (flatVel.magnitude > _moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * _moveSpeed;
                _rb.linearVelocity = new Vector3(limitedVel.x, _rb.linearVelocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        _exitingSlope = true;

        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);

        _rb.AddForce(transform.up * (IsSliding ? _slideJumpForce : _jumpForce), ForceMode.Impulse);
    }

    private void ResetJump()
    {
        _readyToJump = true;

        _exitingSlope = false;
    }

    private bool GroundCheck()
    {
        return Physics.Raycast(transform.position, Vector3.down, _playerHeight * 0.5f + 0.2f, _groundMask);
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out _slopeHit, _playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
            return angle < _maxSlopeAngle && angle != 0;
        }
        return false;
    }

    public Vector3 GetSlopeMoveDir(Vector3 dir)
    {
        return Vector3.ProjectOnPlane(dir, _slopeHit.normal).normalized;
    }

    private void SlidingMovement()
    {
        Vector3 inputDir = transform.forward * _slideDir.y + transform.right * _slideDir.x;

        if (OnSlope() || _rb.linearVelocity.y > -0.1f)
        {
            _rb.AddForce(inputDir.normalized * _moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            _rb.AddForce(GetSlopeMoveDir(inputDir) * _moveSpeed * 10f, ForceMode.Force);
        }

        _slideTimer -= Time.deltaTime;

        if (_slideTimer <= 0)
        {
            UnCrouch();
        }
    }

    private void Crouch(bool sliding)
    {
        if (sliding)
        {
            IsSliding = true;
            _slideTimer = _maxSlideTime;
        }
        else
        {
            _isCrouching = true;
        }
        _playerMesh.localScale = new Vector3(_playerMesh.localScale.x, _crouchYScale, _playerMesh.localScale.z);
        _rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
    }

    private void UnCrouch()
    {
        IsSliding = false;
        _isCrouching = false;
        _playerMesh.localScale = new Vector3(_playerMesh.localScale.x, _startYScale, _playerMesh.localScale.z);
    }

    IEnumerator DelayJumping()
    {
        yield return new WaitForSeconds(0.3f);
        _slideJump = false;
    }

    IEnumerator LerpHeight(float endHeight)
    {
        float elapsedTime = 0;

        while (elapsedTime < _crouchLerpDuration)
        {
            _playerCollider.height = Mathf.Lerp(_playerCollider.height, endHeight, elapsedTime / _crouchLerpDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        _playerCollider.height = endHeight;
    }
}
