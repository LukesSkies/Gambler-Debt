using UnityEngine;

[CreateAssetMenu(menuName = "CaseOpening/Key")]
public class KeyItem : Item
{
    private void OnEnable()
    {
        ItemType = ItemType.Key;
    }
}