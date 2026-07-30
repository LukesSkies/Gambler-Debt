using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class PlayerInputHandler : MonoBehaviour
{
    private GameplayMenus _gameplayMenus;
    private PlayerMove _playerMove;
    private PlayerCamera _playerCamera;
    private PlayerCurrentGun _playerCurrentGun;
    private NewPlayerInteraction _newPlayerInteraction;

    [SerializeField] private PlayerInput _playerInput;
    private InputAction _menuNavigationAction;
    public Vector2 NavigationInput;

    [SerializeField] private float _controllerTimeToInteract;

    [Header("Debug")]
    [SerializeField] private float _controllerTimerToInteract;

    private bool _startControllerInteractTimer = false;
    private bool _startReload = false;
    private bool _shootInputHandled = false;
    private bool _isShootHeld = false;
    private bool _isAimHeld = false;
    private bool _isSprinting = false;
    private bool _interactFired;
    private bool _toggleCrouchController;
    private bool _toggleCrouchKeyboard;

    private void Awake()
    {
        _menuNavigationAction = _playerInput.actions["Navigate"];
    }

    private void Start()
    {
        _gameplayMenus = GameObject.Find("Menus").GetComponent<GameplayMenus>();
        _playerMove = GetComponent<PlayerMove>();
        _playerCamera = transform.Find("CameraHolder").GetComponent<PlayerCamera>();
        _playerCurrentGun = GetComponent<PlayerCurrentGun>();
        _newPlayerInteraction = GetComponent<NewPlayerInteraction>();
    }

    private void Update()
    {
        NavigationInput = _menuNavigationAction.ReadValue<Vector2>();

        GameManager.Instance.Player0IsUsingKeyboardOrMouse = _playerInput.currentControlScheme == "Keyboard&Mouse";

        RaycastGun gun = _playerCurrentGun.CurrentGun.GetComponent<RaycastGun>();

        if (gun.GunSettings.AllowButtonHold && _isShootHeld)
        {
            gun.InputShooting = true;
        }

        gun.Aiming = _isAimHeld;

        if (_playerMove.PlayerDir == Vector2.zero || _playerMove.PlayerDir.y < 0)
        {
            _isSprinting = false;
        }

        _playerMove.SprintQueued = _isSprinting;

        //Controller logic to check if the player wants to reload or interact
        if (_startControllerInteractTimer)
        {
            _controllerTimerToInteract += Time.deltaTime;

            if (_controllerTimerToInteract >= _controllerTimeToInteract && !_interactFired)
            {
                _interactFired = true;
                _startControllerInteractTimer = false;

                if (_newPlayerInteraction.InteractionTest(out IInteractable interactable))
                {
                    if (interactable.CanInteract(_newPlayerInteraction.InteractText))
                    {
                        _newPlayerInteraction.InteractQueued = true;
                    }
                }
                else
                {
                    gun.ReloadQueued = true;
                }
            }
        }

        if (_startReload)
        {
            _startReload = false;

            if (!_interactFired)
            {
                gun.ReloadQueued = true;
            }
        }

        //Uncrouch when player switches from controller to keyboard and they are not currently holding the crouch key
        if(_toggleCrouchController && !_toggleCrouchKeyboard && GameManager.Instance.Player0IsUsingKeyboardOrMouse)
        {
            _toggleCrouchController = false;
            _playerMove.UnCrouch();
        }
    }

    public void OnPause(CallbackContext ctx)
    {
        if (ctx.started)
        {
            Debug.Log("Paused");
            GameManager.Instance.Player0IsUsingKeyboardOrMouse = _playerInput.currentControlScheme == "Keyboard&Mouse";
            _gameplayMenus.Pause();
        }
    }

    public void OnCrouchController(CallbackContext ctx)
    {
        if (ctx.started)
        {
            _toggleCrouchController = !_toggleCrouchController;

            if (_toggleCrouchController)
            {
                _playerMove.Crouch(false);
            }
            else
            {
                _playerMove.UnCrouch();
            }
        }
    }

    public void OnCrouchKeyboard(CallbackContext ctx)
    {
        if (ctx.started)
        {
            _playerMove.Crouch(false);
            _toggleCrouchKeyboard = true;
        }

        if (ctx.canceled)
        {
            _playerMove.UnCrouch();
            _toggleCrouchKeyboard = false;
        }
    }

    public void OnJump(CallbackContext ctx)
    {
        if(ctx.started || ctx.performed && _playerMove.CanJump)
        {
            _playerMove.JumpQueued = true;
        }
    }

    //Controller
    public void OnReloadInteract(CallbackContext ctx)
    {
        if (ctx.started)
        {
            _controllerTimerToInteract = 0f;
            _startControllerInteractTimer = true;
            _interactFired = false;
        }
        if (ctx.canceled)
        {
            _startControllerInteractTimer = false;
            _startReload = true;
        }
    }

    //Keyboard
    public void OnReload(CallbackContext ctx)
    {
        RaycastGun gun = _playerCurrentGun.CurrentGun.GetComponent<RaycastGun>();

        if(ctx.started || ctx.performed)
        {
            gun.ReloadQueued = true;
        }
    }

    //Keyboard
    public void OnInteract(CallbackContext ctx)
    {
        if(ctx.started || ctx.performed)
        {
            if(_newPlayerInteraction.InteractionTest(out IInteractable interactable))
                {
                    if (interactable.CanInteract(_newPlayerInteraction.InteractText))
                    {
                        _newPlayerInteraction.InteractQueued = true;
                    }
                }
        }
    }

    public void OnMove(CallbackContext ctx)
    {
        _playerMove.PlayerDir = ctx.ReadValue<Vector2>();
    }

    public void OnLook(CallbackContext ctx)
    {
        _playerCamera.PlayerLookVector = ctx.ReadValue<Vector2>();
    }

    public void OnShoot(CallbackContext ctx)
    {
        RaycastGun gun = _playerCurrentGun.CurrentGun.GetComponent<RaycastGun>();

        if (gun.GunSettings.AllowButtonHold)
        {
            if (ctx.started || ctx.performed)
            {
                _isShootHeld = true;
            }
            else if (ctx.canceled)
            {
                _isShootHeld = false;
                gun.InputShooting = false;
            }
        }
        else
        {
            if (ctx.started && !_shootInputHandled)
            {
                gun.InputShooting = true;
                _shootInputHandled = true;
            }
            else if (ctx.canceled)
            {
                _shootInputHandled = false;
            }
        }
    }

    public void OnSlide(CallbackContext ctx)
    {
        if(ctx.started)
        {
            _playerMove.TrySlide();
        }
    }

    public void OnAim(CallbackContext ctx)
    {
        _isAimHeld = ctx.ReadValueAsButton();
    }

    //Keyboard
    public void OnSprint(CallbackContext ctx)
    {
        if (ctx.started)
        {
            _isSprinting = true;
        }
        else if (ctx.canceled)
        {
            _isSprinting = false;
        }
    }

    //Controller
    public void OnSprintToggle(CallbackContext ctx)
    {
        if (ctx.started)
        {
            _isSprinting = true;
        }
    }

    //Keyboard
    public void OnSwitchToPrimaryGun(CallbackContext ctx)
    {
        if (ctx.started && _playerCurrentGun.CurrentGunIsSecondary())
        {
            _playerCurrentGun.SwitchToPrimaryGun = true;
        }
    }

    //Keyboard
    public void OnSwitchToSecondaryGun(CallbackContext ctx)
    {
        if (ctx.started && _playerCurrentGun.CurrentGunIsSecondary())
        {
            _playerCurrentGun.SwitchToPrimaryGun = true;
        }
    }

    public void OnSwitchAnyGunController(CallbackContext ctx)
    {
        if(ctx.started && !_playerCurrentGun.GunSwitching)
        {
            _playerCurrentGun.SwitchToAnyGunController = true;
        }
    }

    public void OnSwitchAnyGunMouse(CallbackContext ctx)
    {
        if(ctx.ReadValue<float>() != 0)
        {
            _playerCurrentGun.SwitchToAnyGunMouse = true;
        }
        else
        {
            _playerCurrentGun.SwitchToAnyGunMouse = false;
        }
    }
}