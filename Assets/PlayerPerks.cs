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

    public List<string> ActivePerkList = new List<string>();
    public List<GameObject> ActivePerkListIcons = new List<GameObject>();

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
                ActivePerkListIcons.Add(currentPerkIcon);
                currentPerkIcon.name = perkName;
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

    public void RemovePerksDeath()
    {
        //If the player does not have any perks
        if(ActivePerkList.Count == 0)
        {
            return;
        }

        //If the player only has one perk
        else if(ActivePerkList.Count == 1)
        {
            RemovePerk(ActivePerkList[0]);
            ActivePerkList.Remove(ActivePerkList[0]);
            return;
        }

        //If the player only has two perks
        if (ActivePerkList.Count == 2)
        {
            RemovePerk(ActivePerkList[0]);
            ActivePerkList.Remove(ActivePerkList[0]);
            RemovePerk(ActivePerkList[0]);
            ActivePerkList.Remove(ActivePerkList[0]);
            return;
        }

        //If the player has 3 or more perks
        else
        {
            int perk1 = Random.Range(0, ActivePerkList.Count - 1);
            RemovePerk(ActivePerkList[perk1]);
            ActivePerkList.Remove(ActivePerkList[perk1]);
            int perk2 = Random.Range(0, ActivePerkList.Count - 1);
            RemovePerk(ActivePerkList[perk2]);
            ActivePerkList.Remove(ActivePerkList[perk2]);
        }
    }

    private void RemovePerk(string perkName)
    {
        switch (perkName)
        {
            case "Extra Health":
                TypeOfPerks[0].Active = false;
                RemovePerkIcon("Extra Health");
                break;
            case "Extra Stamina":
                TypeOfPerks[1].Active = false;
                RemovePerkIcon("Extra Stamina");
                break;
            case "Quick Reload":
                TypeOfPerks[2].Active = false;
                RemovePerkIcon("Quick Reload");
                break;
            default:
                break;
        }
    }

    private void RemovePerkIcon(string perkName)
    {
        for (int i = 0; i < ActivePerkListIcons.Count; i++)
        {
            if (ActivePerkListIcons[i].name == perkName)
            {
                ActivePerkListIcons[i].name = "PerkImage" + i;
                Image perkIconImage = ActivePerkListIcons[i].GetComponent<Image>();
                perkIconImage.sprite = null;
                ActivePerkListIcons[i].SetActive(false);
                ActivePerkListIcons.Remove(ActivePerkListIcons[i]);
            }
        }
    }
}
