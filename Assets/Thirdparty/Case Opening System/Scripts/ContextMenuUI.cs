using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ContextMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject _buttonPrefab;
    [SerializeField] private Transform _content;
    [SerializeField] private GameObject _contexPanel;

    private static ContextMenuUI _instance;
    private Inventory _inventory;

    private void Awake()
    {
        _inventory = GetComponent<Inventory>();
        if (_inventory == null)
        {
            this.enabled = false;
            return;
        }

        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        Hide();
    }

    private void Update()
    {
        if (_contexPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI(_contexPanel))
            {
                Hide();
            }
        }
    }

    public static void Show(Vector2 position, Item item)
    {
        if (_instance == null)
        {
            Debug.LogError("No ContextMenuUI in scene!");
            return;
        }

        _instance.BuildMenu(item);
        _instance._contexPanel.SetActive(true);

        RectTransform panelRect = _instance._contexPanel.GetComponent<RectTransform>();
        panelRect.position = position + new Vector2(0, 10);
    }

    public static void Hide()
    {
        if (_instance != null)
        {
            _instance._contexPanel.SetActive(false);

            foreach (Transform child in _instance._content)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void BuildMenu(Item item)
    {
        foreach (Transform child in _content)
        {
            Destroy(child.gameObject);
        }

        List<string> options = new List<string>();

        if (item.ItemType == ItemType.Case)
        {
            options.Add("Inspect");
            options.Add("Open");
            options.Add("Delete");
        }
        else
        {
            options.Add("Inspect");
            options.Add("Delete");
        }

        foreach (string option in options)
        {
            GameObject btnObj = Instantiate(_buttonPrefab, _content);
            TMP_Text text = btnObj.GetComponentInChildren<TMP_Text>();
            text.text = option;

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnButtonClicked(option, item));
        }
    }

    private void OnButtonClicked(string action, Item item)
    {
        if (action == "Inspect")
        {
            OnInspect(item);
        }
        else if (action == "Open")
        {
            OnOpen(item);
        }
        else if (action == "Delete")
        {
            OnDelete(item);
        }

        Hide();
    }

    private bool IsPointerOverUI(GameObject target)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == target || result.gameObject.transform.IsChildOf(target.transform))
            {
                return true;
            }
        }

        return false;
    }

    public void OnInspect(Item item)
    {
        _inventory.InspectItem(item);
    }

    public void OnOpen(Item item)
    {
        _inventory.OpenItem(item);
    }

    public void OnDelete(Item item)
    {
        _inventory.DeleteItem(item);
    }
}