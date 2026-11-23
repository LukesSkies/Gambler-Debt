using UnityEngine;

[CreateAssetMenu(fileName = "New Perk Bar",menuName = "Perks/Perk Bar Definition")]
public class PerkBarDefinition : ScriptableObject
{
    [Header("Bar Information")]
    [SerializeField] private string barName;
    [SerializeField] private Sprite barSprite;

    [Header("Available Perks")]
    [Tooltip("Add the six perks belonging to this bar.")]
    [SerializeField] private PerkDefinition[] perks = new PerkDefinition[6];

    public string BarName => barName;
    public Sprite BarSprite => barSprite;
    public PerkDefinition[] Perks => perks;
}