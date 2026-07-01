using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ItemRegistry
{
    public static Item[] Items { get; private set; }

    public static void LoadAllItems()
    {
        Items = Resources.LoadAll<Item>("");
    }

    public static Item GetItemById(int id)
    {
        return Items?.FirstOrDefault(i => i.Id == id);
    }

    public static Item[] GetItemsSortedById()
    {
        return Items == null ? Array.Empty<Item>() : Items.OrderBy(item => item.Id).ToArray();
    }

    public static Item[] GetItemsSortedByType()
    {
        return Items == null ? Array.Empty<Item>() : Items.OrderBy(item => item.ItemType).ToArray();
    }

    public static int GetFreeID(Item currentItem)
    {
        List<Item> allItems = new List<Item>(Resources.LoadAll<Item>(""));
        HashSet<int> usedIDs = new HashSet<int>();

        foreach (var item in allItems)
        {
            if (item != currentItem)
            {
                usedIDs.Add(item.Id);
            }
        }

        if (!usedIDs.Contains(currentItem.Id))
        {
            return currentItem.Id;
        }

        int id = 0;
        while (usedIDs.Contains(id))
        {
            id++;
        }

        return id;
    }
}