using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform _cellsPanel;
    [SerializeField] private GameObject _cellPrefab;

    private List<Cell> _cells = new List<Cell>();
    private InspectItem _inspectItem;
    private CaseOpeningUI _caseOpeningUI;

    private void Awake()
    {
        _inspectItem = GetComponent<InspectItem>();
        _caseOpeningUI = GetComponent<CaseOpeningUI>();

        ItemRegistry.LoadAllItems();
    }

    public void AddItem(Item item)
    {
        if (item != null)
        {
            Cell cell = Instantiate(_cellPrefab, _cellsPanel).GetComponent<Cell>();
            cell.Refresh(item);
            _cells.Add(cell);
        }
    }

    public void DeleteItem(Item item)
    {
        if (item != null)
        {
            foreach (var cell in _cells)
            {
                if (item == cell.Item)
                {
                    _cells.Remove(cell);
                    Destroy(cell.gameObject);
                    break;
                }
            }
        }
    }

    public void InspectItem(Item item)
    {
        if (item != null && _inspectItem != null)
        {
            _inspectItem.Show(item);
        }
    }

    public void OpenItem(Item item)
    {
        if (item != null)
        {
            CaseItem caseItem = item as CaseItem;

            if (caseItem != null)
            {
                bool haveKey = false;
                KeyItem keyItem = null;

                if (caseItem.RelatedKey == null)
                {
                    haveKey = true;
                }
                else
                {
                    foreach (var cell in _cells)
                    {
                        Item cellItem = cell.Item;
                        if (cellItem != null)
                        {
                            KeyItem key = cellItem as KeyItem;
                            if (key != null && key.Id == caseItem.RelatedKey.Id)
                            {
                                haveKey = true;
                                keyItem = key;
                                break;
                            }
                        }
                    }
                }

                if (haveKey)
                {
                    Item drop = CaseDropSystem.GetRandomDrop(caseItem);

                    if (drop != null)
                    {
                        AddItem(drop);

                        _inspectItem.Show(drop);
                        _caseOpeningUI.BeginRoll(caseItem, drop);

                        DeleteItem(caseItem);
                        if (keyItem != null) DeleteItem(keyItem);
                    }
                    else
                    {
                        Debug.Log("Drop is null");
                    }
                }
                else
                {
                    Debug.Log("Key not found");
                }
            }
        }
    }

    // Save & Load 
    public void Save(string fileName)
    {
        string folderPath = Application.persistentDataPath;
        string fullPath = Path.Combine(folderPath, fileName + ".json");

        SaveSystem.Save(fullPath, _cells);
    }

    public void Load(string fileName)
    {
        string folderPath = Application.persistentDataPath;
        string fullPath = Path.Combine(folderPath, fileName + ".json");

        List<Item> loadedItems = SaveSystem.Load(fullPath);

        if (loadedItems != null)
        {
            foreach (var cell in _cells)
            {
                Destroy(cell.gameObject);
            }
            _cells.Clear();

            foreach (var item in loadedItems)
            {
                AddItem(item);
            }
        }
    }
}