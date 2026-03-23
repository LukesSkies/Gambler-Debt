using UnityEngine;

public class SwapGunMethod : MonoBehaviour
{
    private PlayerCurrentGun _playerCurrentGun;
    private Gun _gun;

    void Awake()
    {
        _playerCurrentGun = transform.root.GetComponent<PlayerCurrentGun>();
        _gun = transform.parent.GetComponent<Gun>();
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
