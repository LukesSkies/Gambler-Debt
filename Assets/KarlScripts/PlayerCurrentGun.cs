using System.Collections.Generic;
using UnityEngine;

public class PlayerCurrentGun : MonoBehaviour
{
    public GameObject CurrentGun;
    public GameObject NextGun;

    public Transform GunHolder;
    private List<GameObject> _gunList = new List<GameObject>();

    private string _primaryGun;
    private string _secondaryGun;

    [HideInInspector] public Animator GunAnimator;

    public bool GunSwitching;
    public bool CanShoot;
    public bool SwitchToPrimaryGun;
    public bool SwitchToSecondGun;
    public bool SwitchToAnyGunController;
    public bool SwitchToAnyGunMouse;

    private void Awake()
    {
        GunHolder = transform.Find("CameraHolder").transform.Find("CameraRecoil").
            transform.Find("GunCamera").transform.Find("WeaponHolder");
    }

    private void Start()
    {
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

        if(NextGun != null)
        {
            _secondaryGun = NextGun.name;
        }

        GunAnimator = CurrentGun.transform.Find("WeaponMesh").GetComponent<Animator>();

        GunSwitching = false;
    }

    private void Update()
    {
        if(NextGun != null)
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

    public void WallBuyAmmo(bool mainGun)
    {
        if (mainGun)
        {
            CurrentGun.GetComponent<RaycastGun>().ReserveAmmo = CurrentGun.GetComponent<RaycastGun>().GunSettings.ReserveAmmo;
            CurrentGun.GetComponent<RaycastGun>().UpdateHUD();
        }
        else
        {
            NextGun.GetComponent<RaycastGun>().ReserveAmmo = NextGun.GetComponent<RaycastGun>().GunSettings.ReserveAmmo;
        }
    }
}
