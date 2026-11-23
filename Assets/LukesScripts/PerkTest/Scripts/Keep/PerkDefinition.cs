using UnityEngine;

[CreateAssetMenu(
    fileName = "New Perk",
    menuName = "Perks/Perk Definition"
)]
public class PerkDefinition : ScriptableObject
{
    [Header("Perk Information")]
    [SerializeField] private string perkName;

    [Header("Tier Images")]
    [Tooltip("Element 0 = Tier 1, Element 1 = Tier 2, Element 2 = Tier 3.")]
    [SerializeField] private Sprite[] tierSprites = new Sprite[3];

    [Header("Inspect Display")]
    [Tooltip("The unique artwork displayed when this perk is hovered.")]
    [SerializeField] private Sprite inspectSprite;

    [Tooltip("Scales this perk's inspect artwork without distorting its proportions.")]
    [Min(0.1f)]
    [SerializeField] private float inspectScale = 1f;

    [Header("Cooldown")]
    [Tooltip("Set to 0 for perks without a cooldown.")]
    [Min(0f)]
    [SerializeField] private float cooldownDuration = 0f;

    [Header("Descriptions")]
    [TextArea(2, 5)]
    [SerializeField] private string[] tierDescriptions = new string[3];

    public string PerkName =>
        string.IsNullOrWhiteSpace(perkName) ? name : perkName;

    public Sprite InspectSprite => inspectSprite;

    public float InspectScale => inspectScale;

    public float CooldownDuration => cooldownDuration;

    public Sprite GetTierSprite(int tier)
    {
        if (tierSprites == null || tierSprites.Length == 0)
            return null;

        int index = Mathf.Clamp(tier - 1, 0, tierSprites.Length - 1);

        return tierSprites[index];
    }

    public string GetTierDescription(int tier)
    {
        if (tierDescriptions == null || tierDescriptions.Length == 0)
            return "";

        int index = Mathf.Clamp(tier - 1, 0, tierDescriptions.Length - 1);

        return tierDescriptions[index];
    }
}