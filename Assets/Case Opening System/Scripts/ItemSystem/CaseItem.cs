using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CaseOpening/Case")]
public class CaseItem : Item
{
    public KeyItem RelatedKey;
    [HideInInspector] public List<SkinItem> Drops;

    private void OnEnable()
    {
        ItemType = ItemType.Case;
    }
}