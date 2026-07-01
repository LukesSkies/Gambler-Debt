using System.Collections.Generic;
using UnityEngine;

public class CaseDropSystem : MonoBehaviour
{
    private static readonly Dictionary<SkinRarity, float> _skinDropChance = new Dictionary<SkinRarity, float>
    {
        { SkinRarity.MilSpec, 79.92f },
        { SkinRarity.Restricted, 15.98f },
        { SkinRarity.Classified, 3.2f },
        { SkinRarity.Covert, 0.64f },
        { SkinRarity.Rare, 0.26f },
    };

    private static float _statTrakChance = 10;

    public static Item GetRandomDrop(CaseItem caseItem)
    {
        return GetSkinDrop(caseItem);
    }

    private static Item GetSkinDrop(CaseItem caseItem)
    {
        SkinRarity chosenRarity = RollRarity(_skinDropChance);
        List<SkinItem> candidates = new List<SkinItem>();

        foreach (var item in caseItem.Drops)
        {
            if (item is SkinItem skin && skin.SkinRarity == chosenRarity)
            {
                candidates.Add(skin);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"In the case {caseItem.Name} no skins of rarity {chosenRarity}");
            return null;
        }

        SkinItem chosenItem = Instantiate(candidates[Random.Range(0, candidates.Count)]);
        chosenItem.StatTrak = Random.Range(0f, 100f) < _statTrakChance;
        chosenItem.SkinWear = (SkinWear)Random.Range(0, System.Enum.GetValues(typeof(SkinWear)).Length);

        chosenItem.Float = Random.Range(0f, 1f);

        return chosenItem;
    }

    private static TEnum RollRarity<TEnum>(Dictionary<TEnum, float> chances)
    {
        float roll = Random.Range(0f, 100f);
        float cumulative = 0f;

        foreach (var pair in chances)
        {
            cumulative += pair.Value;
            if (roll <= cumulative)
            {
                return pair.Key;
            }
        }

        foreach (var key in chances.Keys)
            return key;

        return default;
    }
}