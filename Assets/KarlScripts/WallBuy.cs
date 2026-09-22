using TMPro;
using UnityEngine;

public class WallBuy : MonoBehaviour, IInteractable
{
    [SerializeField] private int _pointCostBuy;
    [SerializeField] private int _pointCostAmmo;
    [SerializeField] private string _gunName;

    [Header("Referances")]
    [SerializeField] private GameObject _activatedGun;
    private Animation _wallbuyGunAnim;
    private bool _wallbuyActive;

    private void Awake()
    {
        if(_activatedGun != null)
        {
            _wallbuyGunAnim = _activatedGun.GetComponent<Animation>();
        }
    }

    void Start()
    {
        _wallbuyActive = false;
        _activatedGun.SetActive(false);
    }

    void Update()
    {
        if(_wallbuyActive == true && !_activatedGun.activeSelf)
        {
            _activatedGun.SetActive(true);
            _wallbuyGunAnim.Play();
        }
    }

    public bool CanInteract(TextMeshProUGUI interactText, NewPlayerInteraction playerInteraction)
    {
        string currentGun = playerInteraction.GetComponent<PlayerCurrentGun>().CurrentGun.name;
        string secondGun = playerInteraction.gameObject.GetComponent<PlayerCurrentGun>().NextGun.name;

        //If the player does not have the wall buy gun
        if(currentGun != _gunName + "(Clone)" && secondGun != _gunName + "(Clone)")
        {
            interactText.text = "Press F to Buy "+ _gunName + " [Cost: " + _pointCostBuy + "]";
            interactText.gameObject.SetActive(true);
            return true;
        }
        //If the player already has the wall buy gun
        else if (currentGun == _gunName + "(Clone)" || secondGun == _gunName + "(Clone)")
        {
            interactText.text = "Press F to Buy Ammo [Cost: " + _pointCostAmmo + "]";
            interactText.gameObject.SetActive(true);
            return true;
        }

        interactText.gameObject.SetActive(false);
        return false;
    }

    public bool Interact(NewPlayerInteraction playerInteraction)
    {
        string currentGun = playerInteraction.GetComponent<PlayerCurrentGun>().CurrentGun.name;
        string secondGun = playerInteraction.gameObject.GetComponent<PlayerCurrentGun>().NextGun.name;

        Points playerPoints = playerInteraction.GetComponent<Points>();

        //If the player already has the wall buy gun
        if (playerPoints.Money >= _pointCostAmmo && (currentGun == _gunName + "(Clone)" || secondGun == _gunName + "(Clone)"))
        {
            if(currentGun == _gunName + "(Clone)")
            {
                playerInteraction.GetComponent<PlayerCurrentGun>().WallBuyAmmo(true, _pointCostAmmo);
            }
            else
            {
                playerInteraction.GetComponent<PlayerCurrentGun>().WallBuyAmmo(false, _pointCostAmmo);
            }
            return true;
        }
        //If the player does not have the wall buy gun
        else if (playerPoints.Money >= _pointCostBuy && (currentGun != _gunName + "(Clone)" && secondGun != _gunName + "(Clone)"))
        {
            PlayerCurrentGun playerCurrentGun = playerInteraction.GetComponent<PlayerCurrentGun>();

            Transform playerGunSpawn = playerCurrentGun.GunHolder;

            GameObject wallBuyGun;

            switch (_gunName)
            {
                case "P90":
                    wallBuyGun = Instantiate(GameManager.Instance.GunGameObjects[1], playerGunSpawn);
                    break;
                default:
                    wallBuyGun = Instantiate(GameManager.Instance.GunGameObjects[1], playerGunSpawn);
                    break;
            }

            playerPoints.RemovePoints(_pointCostBuy);

            Destroy(playerCurrentGun.CurrentGun);

            playerCurrentGun.CurrentGun = wallBuyGun;
            wallBuyGun.SetActive(true);
            playerCurrentGun.GunAnimator = playerCurrentGun.CurrentGun.transform.Find("WeaponMesh").GetComponent<Animator>();

            _wallbuyActive = true;
        }
        return true;
    }
}
