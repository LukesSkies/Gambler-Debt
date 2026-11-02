using UnityEngine;

public class UpgradeWeaponAnimationEvents : MonoBehaviour
{
    [SerializeField] private UpgradeWeapon _upgradeWeapon;
    public void CanTakeWeapon()
    {
        _upgradeWeapon.CanTakeWeapon = true;
    }

    public void StartAnimation()
    {
        _upgradeWeapon.UpgradeAnimation.Play("UpgradeWeaponMoveBack");
    }
}
