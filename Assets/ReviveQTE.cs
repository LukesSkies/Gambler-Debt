using TMPro;
using UnityEngine;

public class ReviveQTE : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;

    private bool _checkingInput;

    private Transform _point;
    private Transform _pointStart;
    private Transform _pointEnd;
    private RectTransform _safeZone;

    private Vector3 targetPos;

    public bool InputQueued;
    public bool InReviveQTE;

    [SerializeField] private int _startingAttempts;
    [SerializeField] private int _currentAttempts;

    private GameplayMenus _gameplayMenus;

    private TextMeshProUGUI _attemptsText;

    private PlayerHP _playerHP;

    private void Awake()
    {
        _point = transform.Find("Point");
        _pointStart = transform.Find("PointStart");
        _pointEnd = transform.Find("PointEnd");
        _safeZone = transform.Find("Bar").transform.GetChild(0).GetComponent<RectTransform>();
        _gameplayMenus = transform.root.GetComponent<GameplayMenus>();
        _attemptsText = transform.Find("AttemptsText").GetComponent<TextMeshProUGUI>();
        _playerHP = GameObject.Find("NewPlayer0").GetComponent<PlayerHP>();
    }

    private void Start()
    {
        ResetMinigame();
    }

    private void Update()
    {
        if (_attemptsText.text != "Attempts: " + _currentAttempts)
        {
            _attemptsText.text = "Attempts: " + _currentAttempts;
        }

        if (!_checkingInput)
        {
            _point.localPosition = Vector3.MoveTowards(_point.localPosition, targetPos, _moveSpeed * 10 * Time.deltaTime);

            if(Vector3.Distance(_point.localPosition, _pointStart.localPosition) < 0.1f)
            {
                targetPos = _pointEnd.localPosition;
            }
            else if (Vector3.Distance(_point.localPosition, _pointEnd.localPosition) < 0.1f)
            {
                targetPos = _pointStart.localPosition;
            }
        }

        if (InputQueued)
        {
            InputQueued = false;
            CheckInput();
        }
    }

    public void ResetMinigame()
    {
        if (!InReviveQTE)
        {
            InReviveQTE = true;
            if(_playerHP.AmountOfDowns <= 4)
            {
                _currentAttempts = _startingAttempts - _playerHP.AmountOfDowns + 1;
            }
            else
            {
                _currentAttempts = 1;
            }
            if(_playerHP.AmountOfDowns > 1)
            {
                _moveSpeed = _moveSpeed + (5 * _playerHP.AmountOfDowns);
            }
        }

        int random = Random.Range(0, 2);

        if(random == 0)
        {
            targetPos = _pointStart.localPosition;
        }
        else
        {
            targetPos = _pointEnd.localPosition;
        }

        _point.localPosition = new Vector3(Random.Range(_pointStart.localPosition.x, _pointEnd.localPosition.x), _point.localPosition.y, _point.localPosition.z);

        _checkingInput = false;
    }

    private void CheckInput()
    {
        _checkingInput = true;

        //If the point is in the safe zone
        if (RectTransformUtility.RectangleContainsScreenPoint(_safeZone, _point.position, null))
        {
            Debug.Log("Hit In Safe Zone");
            GameManager.Instance.PlayerDead = false;
            _gameplayMenus.PlayerCurrentGun.CurrentGun.SetActive(true);
            InReviveQTE = false;
            _playerHP.Health = _playerHP.MaxHealth;
            _playerHP.DeathToggle = false;
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Hit Not In Safe Zone");
            _currentAttempts--;
            if(_currentAttempts <= 0)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
                _gameplayMenus.GameOverMenu.SetActive(true);
                gameObject.SetActive(false);
            }
            ResetMinigame();
        }
    }
}
