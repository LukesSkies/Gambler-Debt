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

    private Animator _gunAnimator;

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

        _gunAnimator = CurrentGun.transform.Find("WeaponMesh").GetComponent<Animator>();

        GunSwitching = false;
    }

    private void Update()
    {
        if(NextGun != null)
        {
            if (SwitchToSecondGun && !GunSwitching)
            {
                _gunAnimator.SetTrigger("PutAway");
                GunSwitching = true;
                SwitchToSecondGun = false;
            }
            else if (SwitchToPrimaryGun && !GunSwitching)
            {
                _gunAnimator.SetTrigger("PutAway");
                GunSwitching = true;
                SwitchToPrimaryGun = false;
            }
            else if (SwitchToAnyGunController)
            {
                _gunAnimator.SetTrigger("PutAway");
                GunSwitching = true;
                SwitchToAnyGunController = false;
            }
            else if (SwitchToAnyGunMouse && !GunSwitching)
            {
                _gunAnimator.SetTrigger("PutAway");
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
        _gunAnimator = CurrentGun.transform.Find("WeaponMesh").GetComponent<Animator>();
        GunSwitching = false;
    }
}
