using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class ItemSaveData
{
    public List<SavedItem> SavedItems = new List<SavedItem>();
}

[System.Serializable]
public class SavedItem
{
    public int Id;

    // Gun-specific
    public string SkinWear;
    public float Float;
    public bool StatTrak;
}

public static class SaveSystem
{
    public static void Save(string fullPath, List<Cell> cells)
    {
        ItemSaveData itemSaveData = new ItemSaveData();
        List<SavedItem> savedItems = itemSaveData.SavedItems;

        foreach (var cell in cells)
        {
            if (cell != null)
            {
                Item item = cell.Item;

                if (item != null)
                {
                    SavedItem saveFile = new SavedItem
                    {
                        Id = item.Id,
                    };

                    if (item.ItemType == ItemType.Skin)
                    {
                        SkinItem skinItem = item as SkinItem;

                        saveFile.SkinWear = skinItem.SkinWear.ToString();
                        saveFile.Float = skinItem.Float;
                        saveFile.StatTrak = skinItem.StatTrak;
                    }

                    savedItems.Add(saveFile);
                }
            }
        }

        string json = JsonUtility.ToJson(itemSaveData, true);

        File.WriteAllText(fullPath, json);
        Debug.Log($"Saved in {fullPath}");
    }

    public static List<Item> Load(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            Debug.Log($"Save file not found: {fullPath}");
            return null;
        }

        string json = File.ReadAllText(fullPath);
        ItemSaveData saveData = JsonUtility.FromJson<ItemSaveData>(json);

        List<Item> loadedItems = new List<Item>();

        foreach (SavedItem saved in saveData.SavedItems)
        {
            Item item = ItemRegistry.GetItemById(saved.Id);

            if (item == null)
            {
                Debug.LogError($"Item with ID {saved.Id} not found in registry. Loading aborted.");
                return null;
            }

            if (item is SkinItem skin)
            {
                if (System.Enum.TryParse(saved.SkinWear, out SkinWear wear))
                    skin.SkinWear = wear;

                skin.Float = saved.Float;
                skin.StatTrak = saved.StatTrak;
            }

            loadedItems.Add(item);
        }

        return loadedItems;
    }
}
