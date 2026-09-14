using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PerkInteraction : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class traitClass
    {
        public bool Active = false;
        public string name;
    }

    [SerializeField] private List<traitClass> _typeOfPerks;

    [SerializeField] private string _activePerkName;
    [SerializeField] private int _perkCost;

    public void Start()
    {
        for (int i = 0; i < _typeOfPerks.Count; i++)
        {
            if (_typeOfPerks[i].Active)
            {
                _activePerkName = _typeOfPerks[i].name;
                break;
            }
        }
    }

    public bool CanInteract(TextMeshProUGUI interactText, NewPlayerInteraction playerInteraction)
    {
        PlayerPerks playerPerks = playerInteraction.GetComponent<PlayerPerks>();

        for (int i = 0; i < playerPerks.TypeOfPerks.Count; i++)
        {
            //If the player does not have the perk
            if (playerPerks.TypeOfPerks[i].name == _activePerkName && !playerPerks.TypeOfPerks[i].Active)
            {
                interactText.text = "Press F for " + _activePerkName + " [Cost: " + _perkCost + "]";
                interactText.gameObject.SetActive(true);
                return true;
            }
        }

        interactText.gameObject.SetActive(false);
        return false;
    }

    public bool Interact(NewPlayerInteraction playerInteraction)
    {
        PlayerPerks playerPerks = playerInteraction.GetComponent<PlayerPerks>();
        Points playerPoints = playerInteraction.GetComponent<Points>();

        for (int i = 0; i < playerPerks.TypeOfPerks.Count; i++)
        {
            //If the player does not have the perk
            if (playerPerks.TypeOfPerks[i].name == _activePerkName && !playerPerks.TypeOfPerks[i].Active && playerPoints.Money >= _perkCost)
            {
                playerPoints.RemovePoints(_perkCost);
                playerPerks.TypeOfPerks[i].Active = true;
            }
        }

        return true;
    }
}
