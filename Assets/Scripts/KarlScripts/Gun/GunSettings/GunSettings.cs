using UnityEngine;

[CreateAssetMenu(fileName = "GunSettings", menuName = "Gun/GunSettings")]
public class GunSettings : ScriptableObject
{
    [Header("Gun Settings")]
    public string GunName;
    public enum GunType
    {
        AssultRifle,
        SubMachineGun,
        Shotgun,
        Sniper,
        Launcher,
        SemiAutomaticRifle,
        Special
    }
    public float GunRange; //How far the range of the gun is per shot
    public float TimeBetweenShooting; //Fire Rate
    public float Spread; //Mainly for shotguns
    public float ReloadTime; //Time for the gun to reload
    public float TimeBetweenShots; //Time between each shot being released (burst weapons/shotguns)
    public int MagazineSize; //How Big is the magazine for the gun
    public int ReserveAmmo; //How much ammo does the gun have in reserve
    public int BulletsPerTap; //How many bullets will be spawned in when shooting
    public bool AllowButtonHold; //Is the gun an automatic gun?

    [Header("Bullet Settings")]
    public GameObject Bullet;
    public float ShootForce; //How far does the bullet go
    public float UpwardForce; //How far up does the bullet go
    public float BulletDamage; //How much damage does each bullet do (base damage)
    public float HeadDamageMultiplier; //Multiplier for if the bullet collides with the head
    public float ChestDamageMultiplier; //Multiplier for if the bullet collides with the chest
    public float AbdomenDamageMultiplier; //Multiplier for if the bullet collides with the abdomen
    public float BulletRange; //How big is the collision (0.01 for normal weapons, usually used for explosions)
    public int MaxCollisions; //How many collisions the bullet can have before it is destroyed
    [Range(0, 1)] public float Bounciness;
    public bool UseGravity;
    public bool ExplodeOnTouch;

    [Header("Recoil Settings")]
    public float HipfireRecoilX; //Up and Down
    public float HipfireRecoilY; //Left and Right
    public float HipfireRecoilZ; //Diagonal
    public float AimRecoilX; 
    public float AimRecoilY; 
    public float AimRecoilZ;
    public float Snappiness; //How quickly it goes from the starting rotation to the recoil rotation
    public float ReturnSpeed; //How quickly the recoil returns to the starting rotation
}
