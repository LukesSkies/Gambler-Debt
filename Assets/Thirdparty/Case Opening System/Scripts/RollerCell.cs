using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RollerCell : MonoBehaviour
{
    [SerializeField] private Image _itemIcon;
    [SerializeField] private Image _rarityImage;

    [SerializeField] private TMP_Text _firstNameText;
    [SerializeField] private TMP_Text _secondNameText;

    [SerializeField] private Sprite _rareSprite;

    public void Refresh(Item item)
    {
        if (item != null)
        {
            SetIcon(item);
            SetNameText(item);
            SetRarityColor(item);
        }
    }

    private void SetIcon(Item item)
    {
        if (item.ItemType == ItemType.Skin)
        {
            SkinItem skinItem = item as SkinItem;

            if (skinItem.SkinType == SkinType.Gun)
            {
                _itemIcon.sprite = item.Icon;
            }
            else
            {
                _itemIcon.sprite = _rareSprite;
            }
        }
        else
        {
            _itemIcon.sprite = item.Icon;
        }
    }

    private void SetNameText(Item item)
    {
        if (item.ItemType == ItemType.Skin)
        {
            SkinItem skinItem = item as SkinItem;

            if (skinItem.SkinType == SkinType.Gun)
            {
                _firstNameText.text = skinItem.Weapon.ToString();
                _secondNameText.text = item.Name;
            }
            else
            {
                _firstNameText.text = $"*{skinItem.SkinType}*";
                _secondNameText.text = "";
            }
        }
        else
        {
            _firstNameText.text = item.ItemType.ToString();
            _secondNameText.text = item.Name;
        }
    }

    private void SetRarityColor(Item item)
    {
        if (item.ItemType == ItemType.Skin)
        {
            _rarityImage.gameObject.SetActive(true);
            _rarityImage.color = RarityColors.GetSkinColor((item as SkinItem).SkinRarity);
        }
        else
        {
            _rarityImage.gameObject.SetActive(false);
        }
    }
}
