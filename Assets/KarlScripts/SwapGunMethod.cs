using UnityEngine;

public class SwapGunMethod : MonoBehaviour
{
    private PlayerCurrentGun _playerCurrentGun;
    private RaycastGun _gun;

    void Awake()
    {
        _playerCurrentGun = transform.root.GetComponent<PlayerCurrentGun>();
        _gun = transform.parent.GetComponent<RaycastGun>();
    }

    private void SwitchGun()
    {
        _playerCurrentGun.SwitchGun();
    }

    private void ReloadGun()
    {
        _gun.Reload();
    }
}
