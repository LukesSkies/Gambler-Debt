using UnityEngine;

public class Crosshair : MonoBehaviour
{
    private RectTransform _reticalLines;

    [SerializeField] private float _reticalTime;
    [SerializeField] private float _moveSize;
    private float _defaultSize;
    private float _reticalTimer;

    private PlayerMove _playerMove;

    void Awake()
    {
        _reticalLines = transform.Find("ReticalLines").GetComponent<RectTransform>();
        _playerMove = GameObject.Find("NewPlayer0").GetComponent<PlayerMove>();
    }

    void Start()
    {
        _defaultSize = _reticalLines.sizeDelta.x;
        _reticalLines.sizeDelta = new Vector2(_defaultSize, _defaultSize);
    }

    void Update()
    {
        _reticalTimer = Mathf.Clamp(_reticalTimer, 0f, _reticalTime);

        if (_playerMove.State == PlayerMove.MovementState.walking || _playerMove.State == PlayerMove.MovementState.sprinting || _playerMove.State == PlayerMove.MovementState.outSliding)
        {
            _reticalTimer += Time.deltaTime;
        }
        else
        {
            _reticalTimer -= Time.deltaTime;
        }

        if (IsAiming())
        {
            transform.GetChild(0).gameObject.SetActive(false);
        }
        else
        {
            transform.GetChild(0).gameObject.SetActive(true);
        }

        UpdateRectial();
    }

    private bool IsAiming()
    {
        return _playerMove.GetComponent<PlayerCurrentGun>().CurrentGun.GetComponent<Gun>().Aiming;
    }

    private void UpdateRectial()
    {
        float time = _reticalTimer / _reticalTime;
        _reticalLines.sizeDelta = Vector2.Lerp(new Vector2(_defaultSize, _defaultSize), new Vector2(_moveSize, _moveSize), time);
    }
}
