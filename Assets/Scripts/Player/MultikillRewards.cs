using UnityEngine;

public class MultikillRewards : MonoBehaviour
{
    private PlayerGunSystem gun;

    void Start()
    {
        gun = FindFirstObjectByType<PlayerGunSystem>();
        ComboSystem.Instance.OnMultikill += HandleMultikill;
    }

    void OnDestroy()
    {
        if (ComboSystem.Instance != null)
            ComboSystem.Instance.OnMultikill -= HandleMultikill;
    }

    void HandleMultikill(int count)
    {
        if (gun == null) return;
        if (gun.currentGun == null) return;
        if (gun.ammo <= 0) return;

        // Prevent refund during active firing
        if (gun.IsShooting)
            return;

        int refund = count * 2;

        int missingAmmo = gun.currentGun.maxAmmo - gun.ammo;
        if (missingAmmo <= 0)
            return;

        int actualRefund = Mathf.Min(refund, missingAmmo);

        gun.ammo += actualRefund;
        gun.ui?.UpdateAmmo(gun.ammo, gun.currentGun.maxAmmo);

        Debug.Log($"Multikill refunded {actualRefund} ammo");
    }

}
