using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InspectItem : MonoBehaviour
{
    [SerializeField] private GameObject _inspectItemPanel;

    [SerializeField] private Image _itemIcon;
    [SerializeField] private Image _itemRarityImage;

    [SerializeField] private TMP_Text _itemNameText;
    [SerializeField] private TMP_Text _itemInfoText;

    public void Show(Item item)
    {
        if (item != null)
        {
            _inspectItemPanel.SetActive(true);

            SetItemIcon(item);
            SetItemName(item);
            SetItemRarityColor(item);
            SetItemInfo(item);
        }
    }

    private void SetItemIcon(Item item)
    {
        if (_itemIcon != null)
        {
            _itemIcon.sprite = item.Icon;
        }
    }

    private void SetItemName(Item item)
    {
        if (_itemNameText != null)
        {
            _itemNameText.text = "";

            if (item.ItemType == ItemType.Skin)
            {
                SkinItem skinItem = item as SkinItem;

                string statTrakText = skinItem.StatTrak ? "*StatTrak* " : "";
                _itemNameText.text = statTrakText + $"{skinItem.Weapon.ToString()} | {skinItem.Name}";
            }
            else
            {
                _itemNameText.text = $"{item.ItemType.ToString()} | {item.Name}";
            }
        }
    }

    private void SetItemRarityColor(Item item)
    {
        if (_itemRarityImage != null)
        {
            if (item.ItemType == ItemType.Skin)
            {
                _itemRarityImage.gameObject.SetActive(true);
                _itemRarityImage.color = RarityColors.GetSkinColor((item as SkinItem).SkinRarity);
            }
            else
            {
                _itemRarityImage.gameObject.SetActive(false);
            }
        }
    }

    private void SetItemInfo(Item item)
    {
        if (_itemInfoText != null)
        {
            if (item.ItemType == ItemType.Skin)
            {
                _itemInfoText.gameObject.SetActive(true);

                SkinItem skinItem = item as SkinItem;
                _itemInfoText.text = $"Wear: {skinItem.SkinWear.ToString()}" +
                    $"\nFloat: {skinItem.Float}";
            }
            else
            {
                _itemInfoText.gameObject.SetActive(false);
            }
        }
    }

    public void CloseInspectItemPanel()
    {
        _inspectItemPanel.SetActive(false);
    }
}