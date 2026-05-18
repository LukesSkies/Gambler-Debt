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

    private bool GunSwitching;

    private WeaponSway _weaponSway;

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

        _weaponSway = CurrentGun.GetComponent<WeaponSway>();

        GunSwitching = false;
    }

    private void Update()
    {
        if(NextGun != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha2) && CurrentGun.name != _secondaryGun && !GunSwitching)
            {
                _gunAnimator.SetTrigger("PutAway");
                GunSwitching = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha1) && CurrentGun.name != _primaryGun && !GunSwitching)
            {
                _gunAnimator.SetTrigger("PutAway");
                GunSwitching = true;
            }
            else if (Input.GetAxis("Mouse ScrollWheel") != 0 && !GunSwitching)
            {
                _gunAnimator.SetTrigger("PutAway");
                GunSwitching = true;
            }

        }
    }

    public void SwitchGun()
    {
        GameObject nextGun = NextGun;
        GameObject currentGun = CurrentGun;
        CurrentGun.SetActive(false);
        NextGun.SetActive(true);
        CurrentGun = nextGun;
        NextGun = currentGun;
        _gunAnimator = CurrentGun.transform.Find("WeaponMesh").GetComponent<Animator>();
        GunSwitching = false;
    }
}
