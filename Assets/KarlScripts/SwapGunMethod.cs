using UnityEngine;

public class SwapGunMethod : MonoBehaviour
{
    private PlayerCurrentGun _playerCurrentGun;
    private RaycastGun _gun;
    private Animator _animator;

    void Awake()
    {
        _playerCurrentGun = transform.root.GetComponent<PlayerCurrentGun>();
        _gun = transform.parent.GetComponent<RaycastGun>();
        _animator = GetComponent<Animator>();
    }

    private void SwitchGun()
    {
        _playerCurrentGun.SwitchGun();
    }

    private void ReloadGun()
    {
        _gun.Reload();
    }

    private void ResetAnimator()
    {
        _animator.speed = 1;
    }
}
