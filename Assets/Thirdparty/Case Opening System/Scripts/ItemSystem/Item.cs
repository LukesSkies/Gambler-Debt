using UnityEngine;

public abstract class Item : ScriptableObject
{
    public string Name;
    public Sprite Icon;

    [HideInInspector] public int Id;
    [HideInInspector] public ItemType ItemType;
}