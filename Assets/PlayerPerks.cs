using UnityEngine;

public class PlayerPerks : MonoBehaviour
{
    public bool ExtraHealth;
    public bool ExtraStamina;

    private PlayerHP _playerHP;
    private PlayerMove _playerMove;

    void Awake()
    {
        _playerHP = GetComponent<PlayerHP>();
        _playerMove = GetComponent<PlayerMove>();
    }

    void Start()
    {
        ExtraHealth = false;
    }

    void Update()
    {
        //Extra Health
        if(ExtraHealth && _playerHP.MaxHealth != GameManager.Instance.PlayerExtraHealth)
        {
            _playerHP.MaxHealth = GameManager.Instance.PlayerExtraHealth;
            _playerHP.Health = GameManager.Instance.PlayerExtraHealth;
        }
        else if (!ExtraHealth && _playerHP.MaxHealth != GameManager.Instance.PlayerHealth)
        {
            _playerHP.MaxHealth = GameManager.Instance.PlayerHealth;
            _playerHP.Health = GameManager.Instance.PlayerHealth;
        }

        //Extra Stamina
        if (ExtraStamina && _playerMove.MaxSprintStamina != GameManager.Instance.PlayerExtraStamina)
        {
            _playerMove.MaxSprintStamina = GameManager.Instance.PlayerExtraStamina;
            _playerMove.CurrentSprintStamina = GameManager.Instance.PlayerExtraStamina;
        }
        else if (!ExtraStamina && _playerMove.MaxSprintStamina != GameManager.Instance.PlayerStamina)
        {
            _playerHP.MaxHealth = GameManager.Instance.PlayerStamina;
            _playerHP.Health = GameManager.Instance.PlayerStamina;
        }
    }
}
