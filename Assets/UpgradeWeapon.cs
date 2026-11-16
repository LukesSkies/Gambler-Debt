using TMPro;
using UnityEngine;

public class UpgradeWeapon : MonoBehaviour, IInteractable
{
    [SerializeField] private RaycastGun _currentGun;
    [SerializeField] private int _pointCostBuy;
    public bool GunInMachine;
    public bool CanTakeWeapon;
    public Animation UpgradeAnimation;

    [SerializeField] private string _gunName;
    private GameObject _gunHolderObject;

    public bool CanInteract(TextMeshProUGUI interactText, NewPlayerInteraction playerInteraction)
    {
        if(playerInteraction.GetComponent<PlayerCurrentGun>().CurrentGun != null)
        {
            _currentGun = playerInteraction.GetComponent<PlayerCurrentGun>().CurrentGun.GetComponent<RaycastGun>();
        }

        if(_currentGun != null)
        {
            if (!_currentGun.Upgraded && !GunInMachine)
            {
                interactText.text = "Press F to Upgrade Weapon [Cost: " + _pointCostBuy + "]";
                interactText.gameObject.SetActive(true);
                return true;
            }
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

        if (playerInteraction.GetComponent<PlayerCurrentGun>().CurrentGun != null)
        {
            _currentGun = playerInteraction.GetComponent<PlayerCurrentGun>().CurrentGun.GetComponent<RaycastGun>();
        }

        Points playerPoints = playerInteraction.GetComponent<Points>();

        //If the player had not upgraded the gun and wants to upgrade it
        if(_currentGun != null)
        {
            if (playerPoints.Money >= _pointCostBuy && !_currentGun.Upgraded && !GunInMachine && !CanTakeWeapon)
            {
                _gunName = _currentGun.name;

                playerPoints.RemovePoints(_pointCostBuy);

                Destroy(playerCurrentGun.CurrentGun.gameObject);

                playerCurrentGun.SwitchGun();

                switch (_currentGun.name)
                {
                    case "M1911":
                        _gunHolderObject = Instantiate(GameManager.Instance.GunUpgradeStartingGameObjects[0], UpgradeAnimation.transform);
                        break;
                    case "M1928":
                        _gunHolderObject = Instantiate(GameManager.Instance.GunUpgradeStartingGameObjects[1], UpgradeAnimation.transform);
                        break;
                    case "P90":
                        _gunHolderObject = Instantiate(GameManager.Instance.GunUpgradeStartingGameObjects[2], UpgradeAnimation.transform);
                        break;
                    case "P90(Clone)":
                        _gunHolderObject = Instantiate(GameManager.Instance.GunUpgradeStartingGameObjects[2], UpgradeAnimation.transform);
                        break;

                    default:
                        _gunHolderObject = Instantiate(GameManager.Instance.GunUpgradeStartingGameObjects[0], UpgradeAnimation.transform);
                        break;
                }

                UpgradeAnimation.Play("UpgradeWeapon");

                GunInMachine = true;

                return true;
            }

            else if (GunInMachine && CanTakeWeapon)
            {
                GetUpgradedGun(playerCurrentGun);

                return true;
            }
        }

        else if (GunInMachine && CanTakeWeapon)
        {
            GetUpgradedGun(playerCurrentGun);

            return true;
        }

        return false;
    }

    private void GetUpgradedGun(PlayerCurrentGun playerCurrentGun)
    {
        GunInMachine = false;
        CanTakeWeapon = false;

        UpgradeAnimation.Stop("UpgradeWeaponMoveBack");

        Transform playerGunSpawn = playerCurrentGun.GunHolder;

        GameObject upgradedGun;

        upgradedGun = SpawnUpgradedGun(_gunName, playerGunSpawn);

        Destroy(_gunHolderObject);

        if (playerCurrentGun.CurrentGun == null)
        {
            playerCurrentGun.CurrentGun = upgradedGun;
            playerCurrentGun.GunAnimator = playerCurrentGun.CurrentGun.transform.Find("WeaponMesh").GetComponent<Animator>();
        }

        else if (playerCurrentGun.NextGun == null)
        {
            playerCurrentGun.NextGun = upgradedGun;

            playerCurrentGun.SwitchGun();
        }

        else
        {
            playerCurrentGun.CurrentGun = upgradedGun;
            playerCurrentGun.GunAnimator = playerCurrentGun.CurrentGun.transform.Find("WeaponMesh").GetComponent<Animator>();
        }
    }

    private GameObject SpawnUpgradedGun(string gunName, Transform gunSpawn)
    {
        switch (gunName)
        {
            case "M1911":
                return Instantiate(GameManager.Instance.UpgradedGunGameObjects[0], gunSpawn);
            case "M1928":
                return Instantiate(GameManager.Instance.UpgradedGunGameObjects[1], gunSpawn);
            case "P90":
                return Instantiate(GameManager.Instance.UpgradedGunGameObjects[2], gunSpawn);
            case "P90(Clone)":
                return Instantiate(GameManager.Instance.UpgradedGunGameObjects[2], gunSpawn);

            default:
                return Instantiate(GameManager.Instance.UpgradedGunGameObjects[0], gunSpawn);
        }
    }
}
