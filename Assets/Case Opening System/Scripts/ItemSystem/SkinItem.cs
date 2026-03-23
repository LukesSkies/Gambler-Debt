using UnityEngine;

[CreateAssetMenu(menuName = "CaseOpening/Skin")]
public class SkinItem : Item
{
    public Weapons Weapon;
    public SkinRarity SkinRarity;

    [HideInInspector] public SkinType SkinType;
    [HideInInspector] public SkinWear SkinWear;
    [HideInInspector] public float Float;
    [HideInInspector] public bool StatTrak;

    private void OnEnable()
    {
        ItemType = ItemType.Skin;
    }
}
