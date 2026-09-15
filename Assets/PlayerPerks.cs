using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPerks : MonoBehaviour
{
    [System.Serializable]
    public class traitClass
    {
        public bool Active = false;
        public string name;
    }

    [SerializeField] public List<traitClass> TypeOfPerks;
    [SerializeField] private List<Sprite> _perkIcons;

    [SerializeField] private string _perkName0;
    [SerializeField] private string _perkName1;
    [SerializeField] private string _perkName2;
    [SerializeField] private string _perkName3;
    [SerializeField] private string _perkName4;

    private PlayerHP _playerHP;
    private PlayerMove _playerMove;

    private Transform _perkSlotParent;

    void Awake()
    {
        _playerHP = GetComponent<PlayerHP>();
        _playerMove = GetComponent<PlayerMove>();
        _perkSlotParent = GameObject.Find("HUD").transform.Find("PerkSlots").transform.GetChild(0);
    }

    void Start()
    {
        TypeOfPerks.Add(new traitClass { name = "Extra Health" });
        TypeOfPerks.Add(new traitClass { name = "Extra Stamina" });
        TypeOfPerks.Add(new traitClass { name = "Quick Reload" });
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

    public void AddPerkUI(string perkName)
    {
        for (int i = 0; i < _perkSlotParent.childCount; i++)
        {
            if (!_perkSlotParent.GetChild(i).gameObject.activeSelf)
            {
                GameObject currentPerkIcon = _perkSlotParent.GetChild(i).gameObject;
                Image perkIconImage = currentPerkIcon.GetComponent<Image>();
                currentPerkIcon.SetActive(true);
                switch (perkName)
                {
                    case "Extra Health":
                        perkIconImage.sprite = _perkIcons[0];
                        break;
                    case "Extra Stamina":
                        perkIconImage.sprite = _perkIcons[1];
                        break;
                    case "Quick Reload":
                        perkIconImage.sprite = _perkIcons[2];
                        break;
                    default:
                        break;
                }
                break;
            }
        }
    }
}
