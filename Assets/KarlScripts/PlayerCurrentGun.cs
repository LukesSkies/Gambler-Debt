using System.Collections.Generic;
using UnityEngine;

public class PlayerCurrentGun : MonoBehaviour
{
    public GameObject CurrentGun;
    public GameObject NextGun;
    public GameObject Grenade;
    private List<GameObject> _gunList = new List<GameObject>();

    public Transform GunHolder;
    public Transform GrenadeHolder;

    private string _primaryGun;
    private string _secondaryGun;

    [HideInInspector] public Animator GunAnimator;
    [HideInInspector] public Animator GrenadeAnimator;

    public bool GunSwitching;
    public bool CanShoot;
    public bool CanSwitchGuns = true;
    public bool SwitchToPrimaryGun;
    public bool SwitchToSecondGun;
    public bool SwitchToAnyGunController;
    public bool SwitchToAnyGunMouse;
    public bool GrenadeActive;

    private Points _playerPoints;
    public Grenade GrenadeScript;

    private void Awake()
    {
        GunHolder = transform.Find("CameraHolder").transform.Find("CameraRecoil").
            transform.Find("GunCamera").transform.Find("WeaponHolder");

        _playerPoints = GetComponent<Points>();

        foreach (Transform child in GunHolder)
        {
            _gunList.Add(child.gameObject);
        }

        foreach (GameObject child in _gunList)
        {
            if (child.activeSelf)
            {
                CurrentGun = child;
            }
            else
            {
                NextGun = child;
            }
        }

        _primaryGun = CurrentGun.name;

        if (NextGun != null)
        {
            _secondaryGun = NextGun.name;
        }

        GunAnimator = CurrentGun.transform.Find("WeaponMesh").GetComponent<Animator>();

        GrenadeHolder = transform.Find("CameraHolder").transform.Find("CameraRecoil").transform.Find("GunCamera").transform.Find("GrenadeHolder");
        Grenade = GrenadeHolder.transform.Find("Grenade").gameObject;
        GrenadeAnimator = Grenade.transform.Find("GrenadeMesh").GetComponent<Animator>();
        GrenadeScript = Grenade.GetComponent<Grenade>();
    }

    private void Start()
    {
        GunSwitching = false;
    }

    private void Update()
    {
        if(NextGun != null && !GrenadeActive && CanSwitchGuns)
        {
            if (SwitchToSecondGun && !GunSwitching)
            {
                GunAnimator.SetTrigger("PutAway");
                GunSwitching = true;
                SwitchToSecondGun = false;
            }
            else if (SwitchToPrimaryGun && !GunSwitching)
            {
                GunAnimator.SetTrigger("PutAway");
                GunSwitching = true;
                SwitchToPrimaryGun = false;
            }
            else if (SwitchToAnyGunController)
            {
                GunAnimator.SetTrigger("PutAway");
                GunSwitching = true;
                SwitchToAnyGunController = false;
            }
            else if (SwitchToAnyGunMouse && !GunSwitching)
            {
                GunAnimator.SetTrigger("PutAway");
                GunSwitching = true;
            }
        }
    }

    public bool CurrentGunIsPrimary()
    {
        return CurrentGun.name == _primaryGun;
    }

    public bool CurrentGunIsSecondary()
    {
        return CurrentGun.name == _secondaryGun;
    }

    public void SwitchGun()
    {
        if (GrenadeActive)
        {
            CurrentGun.SetActive(true);
        }
        else
        {
            GameObject nextGun = NextGun;
            GameObject currentGun = CurrentGun;
            CurrentGun.SetActive(false);
            NextGun.SetActive(true);
            CurrentGun = nextGun;
            NextGun = currentGun;
            if (CurrentGun.GetComponent<RaycastGun>())
            {
                CurrentGun.GetComponent<RaycastGun>().UpdateHUD();
            }
            GunAnimator = CurrentGun.transform.Find("WeaponMesh").GetComponent<Animator>();
            GunSwitching = false;
        }
    }

    public void WallBuyAmmo(bool mainGun, int pointCost)
    {
        if (mainGun)
        {
            if(CurrentGun.GetComponent<RaycastGun>().ReserveAmmo < CurrentGun.GetComponent<RaycastGun>().GunSettings.ReserveAmmo)
            {
                CurrentGun.GetComponent<RaycastGun>().ReserveAmmo = CurrentGun.GetComponent<RaycastGun>().GunSettings.ReserveAmmo;
                if (CurrentGun.GetComponent<RaycastGun>())
                {
                    CurrentGun.GetComponent<RaycastGun>().UpdateHUD();
                }
                _playerPoints.RemovePoints(pointCost);
            }
        }
        else
        {
            if(NextGun.GetComponent<RaycastGun>().ReserveAmmo < NextGun.GetComponent<RaycastGun>().GunSettings.ReserveAmmo)
            {
                NextGun.GetComponent<RaycastGun>().ReserveAmmo = NextGun.GetComponent<RaycastGun>().GunSettings.ReserveAmmo;
                _playerPoints.RemovePoints(pointCost);
            }
        }
    }

    public void GrenadePull()
    {
        GrenadeActive = true;
        CanSwitchGuns = false;
        CurrentGun.SetActive(false);
        Grenade.SetActive(true);
        GrenadeAnimator.SetBool("GrenadePull", true);
    }

    public void GrenadeGunReset()
    {
        GrenadeActive = false;
        CanSwitchGuns = true;
        CurrentGun.SetActive(true);
        Grenade.SetActive(false);
    }
}
