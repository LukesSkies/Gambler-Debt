using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PerkOfferButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Button References")]
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Image perkImage;

    [Header("Text References")]
    [SerializeField] private TMP_Text perkNameText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text costText;

    private PerkSystemManager manager;
    private PerkDefinition perk;

    private RectTransform HoverRect
    {
        get
        {
            if (purchaseButton != null)
                return purchaseButton.transform as RectTransform;

            return transform as RectTransform;
        }
    }

    private void Awake()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(HandlePurchase);
    }

    private void OnDestroy()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(HandlePurchase);
    }

    // OFFER SETUP

    public void SetOffer(
        PerkSystemManager systemManager,
        PerkDefinition perkDefinition)
    {
        manager = systemManager;
        perk = perkDefinition;

        Refresh();
    }

    public void ClearOffer()
    {
        if (PerkInspectUI.Instance != null)
            PerkInspectUI.Instance.Hide(HoverRect);

        perk = null;

        if (perkImage != null)
        {
            perkImage.sprite = null;
            perkImage.enabled = false;
        }

        if (perkNameText != null)
            perkNameText.text = "";

        if (tierText != null)
            tierText.text = "";

        if (costText != null)
            costText.text = "";

        if (purchaseButton != null)
            purchaseButton.interactable = false;
    }

    // REFRESH

    public void Refresh()
    {
        if (manager == null || perk == null)
        {
            ClearOffer();
            return;
        }

        int ownedTier = manager.GetOwnedTier(perk);
        int nextTier = ownedTier + 1;

        bool isMaxed = ownedTier >= 3;

        bool alreadyPurchased =
            manager.HasPurchasedThisShopCycle(perk);

        int displayTier = Mathf.Clamp(nextTier, 1, 3);

        if (perkImage != null)
        {
            perkImage.sprite = perk.GetTierSprite(displayTier);
            perkImage.enabled = perkImage.sprite != null;
        }

        if (perkNameText != null)
            perkNameText.text = perk.PerkName;

        if (isMaxed)
        {
            if (tierText != null)
                tierText.text = "MAX TIER";

            if (costText != null)
                costText.text = "MAXED";

            if (purchaseButton != null)
                purchaseButton.interactable = false;

            return;
        }

        if (tierText != null)
            tierText.text = "TIER " + nextTier;

        if (alreadyPurchased)
        {
            if (costText != null)
                costText.text = "PURCHASED";

            if (purchaseButton != null)
                purchaseButton.interactable = false;

            return;
        }

        int cost = PerkSystemManager.GetTierCost(nextTier);

        if (costText != null)
            costText.text = cost.ToString("N0");

        if (purchaseButton != null)
            purchaseButton.interactable = true;
    }

    // MOUSE HOVER

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (manager == null || perk == null)
            return;

        int ownedTier = manager.GetOwnedTier(perk);

        // Show the tier the player would receive next.
        int inspectTier = Mathf.Clamp(ownedTier + 1, 1, 3);

        if (PerkInspectUI.Instance != null)
        {
            PerkInspectUI.Instance.Show(
                perk,
                inspectTier,
                HoverRect
            );
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (PerkInspectUI.Instance != null)
            PerkInspectUI.Instance.Hide(HoverRect);
    }

    // PURCHASE

    private void HandlePurchase()
    {
        if (manager == null || perk == null)
            return;

        if (manager.TryPurchase(perk))
        {
            // Hide the preview after a successful purchase.
            if (PerkInspectUI.Instance != null)
                PerkInspectUI.Instance.Hide(HoverRect);
        }
    }
}