using System.Collections;
using UnityEngine;
public class PlayerMove : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _walkSpeed;
    [SerializeField] private float _sprintSpeed;
    [SerializeField] private float _groundDrag;
    [SerializeField] private float _walkSpeedSmoothness = 12;
    [SerializeField] private float _sprintSpeedSmoothness = 6;
    public float MaxSprintStamina = 4;
    [SerializeField] private float _timePlayerSprintEnabled = 1.5f;

    [Header("Player Step Height")]
    [SerializeField] private float _stepHeight = 0.3f;
    [SerializeField] private float _stepSmooth = 0.1f;
    private GameObject _stepRayLower;
    private GameObject _stepRayHigher;

    [Header("Jumping")]
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _jumpCooldown;
    [SerializeField] private float _airMultiplier;

    [Header("Crouching")]
    [SerializeField] private float _crouchSpeed;
    [SerializeField] private float _crouchHeight;
    [SerializeField] private float _crouchYScale;
    [SerializeField] private float _crouchLerpDuration;
    [SerializeField] private float _crouchScaleSmoothness = 8f;

    [Header("Sliding")]
    [SerializeField] private float _maxSlideTime;
    [SerializeField] private float _slideJumpForce;
    [SerializeField] private float _lerpGunTime;
    [SerializeField] private float _slideDirSmoothness = 5f;
    [SerializeField] private float _slideJumpDelay = 0.2f;

    private float _slideTimer;
    private float _startYScale;
    private bool _readyToJump;
    private float _moveSpeed;
    private float _slideJumpTimer;

    [Header("Ground Check")]
    [SerializeField] private float _playerHeight;
    [SerializeField] private LayerMask _groundMask;

    [Header("Slope Handling")]
    [SerializeField] private float _maxSlopeAngle;
    private RaycastHit _slopeHit;
    private bool _exitingSlope;

    [Header("PlayerStates")]
    public MovementState State;
    public bool CanMove;
    public bool CanJump;
    public bool CanSprint;
    public bool SprintQueued;
    public bool SlidingQueued;
    public bool JumpQueued;

    [Header("PlayerInput")]
    public Vector2 PlayerDir;

    [Header("Player Live Values")]
    public float CurrentSprintStamina = 4;

    private Vector3 _moveDir;
    private Vector3 _smoothMoveDir;

    private Rigidbody _rb;

    private bool _isCrouching;
    [HideInInspector] public bool IsSliding;
    [HideInInspector] public bool JumpSlide;
    private bool _slideJump;
    private bool _slideJumpToggle;

    private Transform _playerMesh;

    private Vector2 _slideDir;
    private Vector2 _smoothSlideDir;

    private Transform _weaponHolder;

    private Coroutine _gunRotationCoroutine;
    private Coroutine _scaleCoroutine;

    [HideInInspector]public enum MovementState 
    {
        walking,
        sprinting,
        crouching,
        sliding,
        outSliding,
        air,
        idle
    }

    private void Awake()
    {
        _weaponHolder = transform.Find("CameraHolder").transform.Find("CameraRecoil").
            transform.Find("GunCamera").transform.Find("WeaponHolder");
        
        _playerMesh = transform.Find("PlayerMesh");

        _stepRayLower = transform.Find("StepRayLower").gameObject;
        _stepRayHigher = transform.Find("StepRayHigher").gameObject;
    }

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
        _readyToJump = true;
        CanJump = false;
        CanSprint = false;
        CanMove = false;
        _startYScale = _playerMesh.localScale.y;
        _stepRayHigher.transform.localPosition = new Vector3(_stepRayHigher.transform.localPosition.x, _stepHeight, _stepRayHigher.transform.localPosition.z);
    }

    void FixedUpdate()
    {
        if (CanMove)
        {
            if (IsSliding)
            {
                SlidingMovement();
            }
            else if (JumpSlide)
            {
                OutSide();
                MovePlayer();

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

            StepClimb();
        }
    }

    void Update()
    {
        if (GameManager.Instance.PlayerDead)
        {
            CanMove = false;
            CanSprint = false;
            CanJump = false;
        }
        else
        {
            CanMove = true;
            CanJump = true;
        }

        JumpInput();
        SpeedControl();
        StateHandler();
        StaminaHandler();

        if (IsSliding)
        {
            _slideJumpTimer += Time.deltaTime;
        }
        else
        {
            _slideJumpTimer = 0f;
        }

        if (GroundCheck())
        {
            _rb.linearDamping = _groundDrag;
        }
        else
        {
            _rb.linearDamping = 0;
        }
    }

    private void JumpInput()
    {
        if (JumpQueued)
        {
            JumpQueued = false;
            if (CanJump && GroundCheck() && _readyToJump && !_isCrouching)
            {
                //Block jump if player hasnt been sliding long enough
                if (IsSliding && _slideJumpTimer < _slideJumpDelay) return;

                _readyToJump = false;

                bool wasSliding = IsSliding;

                if (wasSliding)
                {
                    IsSliding = false;
                    _slideJump = true;
                    JumpSlide = true;
                    _slideJumpToggle = true;

                    if (_scaleCoroutine != null)
                    {
                        StopCoroutine(_scaleCoroutine);
                        _scaleCoroutine = null;
                    }

                    UnCrouch();
                }

                Jump(wasSliding);
                Invoke(nameof(ResetJump), _jumpCooldown);
            }
        }
    }

    private void StateHandler()
    {
        if (IsSliding)
        {
            State = MovementState.sliding;
        }
        else if (JumpSlide)
        {
            State = MovementState.outSliding;
            _moveSpeed = _sprintSpeed;
        }
        else if (_isCrouching)
        {
            State = MovementState.crouching;
            _moveSpeed = _crouchSpeed;
        }

        else if(GroundCheck() && SprintQueued && PlayerDir.y > 0 && CanSprint)
        {
            State = MovementState.sprinting;
            _moveSpeed = _sprintSpeed;
        }

        else if (GroundCheck() && PlayerDir != Vector2.zero)
        {
            State = MovementState.walking;
            _moveSpeed = _walkSpeed;
        }
        else if(GroundCheck())
        {
            State = MovementState.idle;
        }
        else
        {
            State = MovementState.air;
        }
    }

    private void StaminaHandler()
    {
        if(CurrentSprintStamina >= _timePlayerSprintEnabled && GameManager.Instance.PlayerDead != true)
        {
            CanSprint = true;
        }

        else if(CurrentSprintStamina == 0)
        {
            CanSprint = false;
        }

        else if (CurrentSprintStamina < 0)
        {
            CurrentSprintStamina = 0;
        }

        else if (CurrentSprintStamina > MaxSprintStamina)
        {
            CurrentSprintStamina = MaxSprintStamina;
        }

        if (State == MovementState.sprinting && CurrentSprintStamina > 0)
        {
            CurrentSprintStamina -= Time.deltaTime;
        }

        else if (State != MovementState.sprinting && CurrentSprintStamina < MaxSprintStamina)
        {
            CurrentSprintStamina += Time.deltaTime;
        }
    }

    private void MovePlayer()
    {
        float horizontalScale = (State == MovementState.sprinting) ? 0.5f : 1f;
        _moveDir = transform.forward * PlayerDir.y + transform.right * (PlayerDir.x * horizontalScale);

        float lerpSpeed;
        if (State == MovementState.idle && OnSlope())
            lerpSpeed = _walkSpeedSmoothness * 3f;
        else
            lerpSpeed = (State == MovementState.sprinting) ? _sprintSpeedSmoothness : _walkSpeedSmoothness;

        _smoothMoveDir = Vector3.Lerp(_smoothMoveDir, _moveDir, Time.fixedDeltaTime * lerpSpeed);
        Vector3 clampedDir = Vector3.ClampMagnitude(_smoothMoveDir, 1f);

        if (OnSlope())
        {
            _rb.AddForce(GetSlopeMoveDir(clampedDir) * _moveSpeed * 20f, ForceMode.Force);

            if(_rb.linearVelocity.y > 0)
            {
                _rb.AddForce(Vector3.down * 40f, ForceMode.Force);
            }

            Vector3 gravityCounterForce = -Physics.gravity * (1f - Vector3.Dot(Vector3.up, _slopeHit.normal));
            _rb.AddForce(gravityCounterForce, ForceMode.Force);
        }
        else if (GroundCheck())
        {
            _rb.AddForce(clampedDir * _moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            _rb.AddForce(clampedDir * _moveSpeed * 10f * _airMultiplier, ForceMode.Force);
        }

        _rb.useGravity = true;
    }

    private void StepClimb()
    {
        RaycastHit hitLower;
        if(Physics.Raycast(_stepRayLower.transform.position, transform.TransformDirection(Vector3.forward), out hitLower, 0.1f))
        {
            RaycastHit hitHigher;
            if (!Physics.Raycast(_stepRayHigher.transform.position, transform.TransformDirection(Vector3.forward), out hitHigher, 0.2f))
            {
                _rb.position -= new Vector3(0, -_stepSmooth, 0);
            }
        }

        RaycastHit hitLower45;
        if (Physics.Raycast(_stepRayLower.transform.position, transform.TransformDirection(1.5f,0,1f), out hitLower45, 0.1f))
        {
            RaycastHit hitHigher45;
            if (!Physics.Raycast(_stepRayHigher.transform.position, transform.TransformDirection(1.5f, 0, 1f), out hitHigher45, 0.2f))
            {
                _rb.position -= new Vector3(0, -_stepSmooth, 0);
            }
        }

        RaycastHit hitLowerMinus45;
        if (Physics.Raycast(_stepRayLower.transform.position, transform.TransformDirection(-1.5f, 0, 1f), out hitLowerMinus45, 0.1f))
        {
            RaycastHit hitHigherMinus45;
            if (!Physics.Raycast(_stepRayHigher.transform.position, transform.TransformDirection(-1.5f, 0, 1f), out hitHigherMinus45, 0.2f))
            {
                _rb.position -= new Vector3(0, -_stepSmooth, 0);
            }
        }
    }

    private void OutSide()
    {
        _rb.useGravity = true;

        if (GroundCheck() && !_slideJump)
        {
            JumpSlide = false;
            if (SlidingQueued && PlayerDir.y > 0)
            {
                SlidingQueued = false;
                _slideDir = PlayerDir;
                Crouch(true);
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

    private void Jump(bool wasSliding = false)
    {
        _exitingSlope = true;

        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);

        bool reducedJump = GoingDownSlope() && OnSlope() && !GroundCheck() == false;

        _rb.AddForce(transform.up * (reducedJump ? _jumpForce : (wasSliding ? _slideJumpForce : _jumpForce)), ForceMode.Impulse);
    }

    private void ResetJump()
    {
        _readyToJump = true;

        _exitingSlope = false;
    }

    public bool GroundCheck()
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

    public bool GoingDownSlope()
    {
        if (!OnSlope()) return false;

        Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        if (flatVel.magnitude < 0.1f) return false;

        return GetSlopeMoveDir(flatVel.normalized).y < -0.05f;
    }

    public Vector3 GetSlopeMoveDir(Vector3 dir)
    {
        return Vector3.ProjectOnPlane(dir, _slopeHit.normal).normalized;
    }

    private void SlidingMovement()
    {
        _smoothSlideDir = Vector2.Lerp(_smoothSlideDir, _slideDir, Time.fixedDeltaTime * _slideDirSmoothness);

        Vector3 inputDir = transform.forward * _smoothSlideDir.y + transform.right * _smoothSlideDir.x;

        if (OnSlope() || _rb.linearVelocity.y > -0.1f)
        {
            _rb.AddForce(inputDir.normalized * _moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            _rb.AddForce(GetSlopeMoveDir(inputDir) * _moveSpeed * 10f, ForceMode.Force);
        }

        if (_slideTimer <= 0 || _rb.linearVelocity.magnitude < 1.5f)
        {
            UnCrouch();
        }

        _slideTimer -= Time.deltaTime;
    }

    public void Crouch(bool sliding)
    {
        if (sliding)
        {
            IsSliding = true;
            _slideTimer = _maxSlideTime;

            SetGunRotation(new Vector3(0, 0, 30));

            _slideDir = PlayerDir;

            Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z).normalized;
            Vector3 localVel = transform.InverseTransformDirection(flatVel);
            _smoothSlideDir = new Vector2(localVel.x, localVel.z);
        }
        else
        {
            _isCrouching = true;
        }
        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(LerpPlayerScale(_crouchYScale));
        _rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
    }

    public void UnCrouch()
    {
        IsSliding = false;
        _isCrouching = false;
        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(LerpPlayerScale(_startYScale));
        SetGunRotation(Vector3.zero);
    }

    public void TrySlide()
    {
        if (_isCrouching) return;

        if (State == MovementState.outSliding)
        {
            SlidingQueued = true;
        }
        else if (GroundCheck() && State == MovementState.sprinting && PlayerDir.y > 0)
        {
            _slideDir = PlayerDir;
            Crouch(true);
        }
    }

    IEnumerator DelayJumping()
    {
        yield return new WaitForSeconds(0.3f);
        _slideJump = false;
    }

    private void SetGunRotation(Vector3 gunRot)
    {
        if (_gunRotationCoroutine != null)
            StopCoroutine(_gunRotationCoroutine);
        _gunRotationCoroutine = StartCoroutine(LerpGunRotation(gunRot));
    }

    IEnumerator LerpPlayerScale(float targetYScale)
    {
        while (Mathf.Abs(_playerMesh.localScale.y - targetYScale) > 0.001f)
        {
            float newY = Mathf.Lerp(_playerMesh.localScale.y, targetYScale, Time.deltaTime * _crouchScaleSmoothness);
            _playerMesh.localScale = new Vector3(_playerMesh.localScale.x, newY, _playerMesh.localScale.z);
            yield return null;
        }
        _playerMesh.localScale = new Vector3(_playerMesh.localScale.x, targetYScale, _playerMesh.localScale.z);
    }

    IEnumerator LerpGunRotation(Vector3 gunRot)
    {
        float elapsedTime = 0;

        while (elapsedTime < _lerpGunTime)
        {
            _weaponHolder.localRotation = Quaternion.Lerp(_weaponHolder.localRotation, Quaternion.Euler(gunRot), elapsedTime / _lerpGunTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _weaponHolder.localRotation = Quaternion.Euler(gunRot);
    }
}