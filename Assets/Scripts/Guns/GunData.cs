using UnityEngine;

[CreateAssetMenu(menuName = "Guns/New Gun")]
public class GunData : ScriptableObject
{
    [Header("General")]
    public string gunName;
    public GameObject gunPrefab;
    public bool fullAuto = false;
    public bool useRaycast = true;
    public int maxAmmo = 12;
    public float fireRate = 0.2f;
    public float damage = 10f;
    public float maxRange = 50f;

    [Header("Spread / Shotgun")]
    [Range(0f, 1f)] public float spread = 0.05f;
    public int pellets = 1;

    [Header("Recoil & Aim Assist")]
    public float recoilAmount = 2f;
    public float aimAssistRadius = 1.5f;
    public float aimAssistRange = 40f;
    public float magnetismStrength = 0.6f;

    [Header("Penetration")]
    public bool allowPenetration = false;
    public int maxPenetrations = 1;
    public LayerMask hitMask;

    [Header("Range and Falloff")]
    [Tooltip("Full damage inside this range.")]
    public float falloffStartRange = 8f;

    [Tooltip("Zero damage beyond this range.")]
    public float maxEffectiveRange = 20f;
}
