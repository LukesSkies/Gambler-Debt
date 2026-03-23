using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Cell : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public Item Item;

    [SerializeField] private Image _itemIcon;
    [SerializeField] private Image _rarityImage;

    [SerializeField] private TMP_Text _firstNameText;
    [SerializeField] private TMP_Text _secondNameText;

    public void Refresh(Item item)
    {
        if (item != null)
        {
            Item = item;

            SetIcon();
            SetNameText();
            SetRarityColor();
        }
    }

    private void SetIcon()
    {
        _itemIcon.sprite = Item.Icon;
    }

    private void SetNameText()
    {
        if (Item.ItemType == ItemType.Skin)
        {
            SkinItem skinItem = Item as SkinItem;

            string statTrakText = skinItem.StatTrak ? "*StatTrak* " : "";
            _firstNameText.text = statTrakText + skinItem.Weapon.ToString();
            _secondNameText.text = Item.Name;
        }
        else
        {
            _firstNameText.text = Item.ItemType.ToString();
            _secondNameText.text = Item.Name;
        }
    }

    private void SetRarityColor()
    {
        if (Item.ItemType == ItemType.Skin)
        {
            _rarityImage.gameObject.SetActive(true);
            _rarityImage.color = RarityColors.GetSkinColor((Item as SkinItem).SkinRarity);
        }
        else
        {
            _rarityImage.gameObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ContextMenuUI.Show(eventData.position, Item);
        }
    }
}
