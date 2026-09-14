using System.Collections.Generic;
using UnityEngine;

public class PlayerPerks : MonoBehaviour
{
    [System.Serializable]
    public class traitClass
    {
        public bool Active = false;
        public string name;
    }

    [SerializeField] public List<traitClass> TypeOfPerks;

    private PlayerHP _playerHP;
    private PlayerMove _playerMove;

    void Awake()
    {
        _playerHP = GetComponent<PlayerHP>();
        _playerMove = GetComponent<PlayerMove>();
    }

    void Start()
    {
        TypeOfPerks.Add(new traitClass { name = "Extra Health" });
        TypeOfPerks.Add(new traitClass { name = "ExtraStamina" });
    }

    void Update()
    {
        //Extra Health
        if (TypeOfPerks[0].Active && _playerHP.MaxHealth != GameManager.Instance.PlayerExtraHealth)
        {
            _playerHP.MaxHealth = GameManager.Instance.PlayerExtraHealth;
            _playerHP.Health = GameManager.Instance.PlayerExtraHealth;
        }
        else if (!TypeOfPerks[0].Active && _playerHP.MaxHealth != GameManager.Instance.PlayerHealth)
        {
            _playerHP.MaxHealth = GameManager.Instance.PlayerHealth;
            _playerHP.Health = GameManager.Instance.PlayerHealth;
        }

        //Extra Stamina
        if (TypeOfPerks[1].Active && _playerMove.MaxSprintStamina != GameManager.Instance.PlayerExtraStamina)
        {
            _playerMove.MaxSprintStamina = GameManager.Instance.PlayerExtraStamina;
            _playerMove.CurrentSprintStamina = GameManager.Instance.PlayerExtraStamina;
        }
        else if (!TypeOfPerks[1].Active && _playerMove.MaxSprintStamina != GameManager.Instance.PlayerStamina)
        {
            _playerHP.MaxHealth = GameManager.Instance.PlayerStamina;
            _playerHP.Health = GameManager.Instance.PlayerStamina;
        }
    }
}
