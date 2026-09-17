using UnityEngine;

public static class RarityColors
{
    public static Color GetSkinColor(SkinRarity rarity)
    {
        switch (rarity)
        {
            case SkinRarity.Consumer:
                return new Color(0.66f, 0.66f, 0.66f);
            case SkinRarity.Industrial:
                return new Color(0.0f, 0.5f, 1f);
            case SkinRarity.MilSpec:
                return new Color(0.0f, 0.33f, 0.66f);
            case SkinRarity.Restricted:
                return new Color(0.58f, 0.0f, 0.83f);
            case SkinRarity.Classified:
                return new Color(1f, 0.0f, 0.6f);
            case SkinRarity.Covert:
                return new Color(1f, 0.0f, 0.0f);
            case SkinRarity.Rare:
                return new Color(1f, 0.84f, 0f);
            case SkinRarity.Contraband:
                return new Color(1f, 0.5f, 0f);
            default:
                return Color.gray;
        }
    }
}