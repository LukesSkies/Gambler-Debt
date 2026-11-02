using TMPro;
using UnityEngine;

public class UpgradeWeapon : MonoBehaviour, IInteractable
{
    [SerializeField] private RaycastGun _currentGun;
    [SerializeField] private int _pointCostBuy;
    public bool GunInMachine;
    public bool CanTakeWeapon;
    public Animation UpgradeAnimation;

    public bool CanInteract(TextMeshProUGUI interactText, NewPlayerInteraction playerInteraction)
    {
        if(playerInteraction.GetComponent<PlayerCurrentGun>().CurrentGun == null)
        {
            interactText.gameObject.SetActive(false);
            return false;
        }

        _currentGun = playerInteraction.GetComponent<PlayerCurrentGun>().CurrentGun.GetComponent<RaycastGun>();

        if (!_currentGun.Upgraded && !GunInMachine)
        {
            interactText.text = "Press F to Upgrade Weapon [Cost: " + _pointCostBuy + "]";
            interactText.gameObject.SetActive(true);
            return true;
        }

        if(GunInMachine && CanTakeWeapon)
        {
            interactText.text = "Press F to Get Upgraded Gun";
            interactText.gameObject.SetActive(true);
            return true;
        }

        interactText.gameObject.SetActive(false);
        return false;
    }

    public bool Interact(NewPlayerInteraction playerInteraction)
    {
        PlayerCurrentGun playerCurrentGun = playerInteraction.GetComponent<PlayerCurrentGun>();

        _currentGun = playerCurrentGun.CurrentGun.GetComponent<RaycastGun>();

        Points playerPoints = playerInteraction.GetComponent<Points>();

        //If the player had not upgraded the gun and wants to upgrade it
        if (playerPoints.Money >= _pointCostBuy && !_currentGun.Upgraded && !GunInMachine && !CanTakeWeapon)
        {
            playerPoints.RemovePoints(_pointCostBuy);

            Destroy(playerCurrentGun.CurrentGun.gameObject);

            playerCurrentGun.SwitchGun();

            GameObject playerGun;

            //TODO: Play Upgrade Gun Animation

            switch (_currentGun.name)
            {
                case "M1911":
                    playerGun = Instantiate(GameManager.Instance.GunUpgradeStartingGameObjects[0], UpgradeAnimation.transform);
                    break;

                default:
                    playerGun = Instantiate(GameManager.Instance.GunUpgradeStartingGameObjects[0], UpgradeAnimation.transform);
                    break;
            }

            UpgradeAnimation.Play("UpgradeWeapon");

            GunInMachine = true;

            return true;
        }

        else if (GunInMachine && CanTakeWeapon)
        {
            //TODO: Get Upgraded Gun
        }

        return true;
    }
}
