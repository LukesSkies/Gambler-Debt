using UnityEngine;
using UnityEngine.UI;

public class PerkBarUI : MonoBehaviour
{
    [Header("Bar Image")]
    [SerializeField] private Image barImage;

    [Header("Three Perk Offer Buttons")]
    [SerializeField] private PerkOfferButton[] offerButtons = new PerkOfferButton[3];

    private PerkSystemManager manager;

    public PerkDefinition[] CurrentOffers { get; private set; } =
        new PerkDefinition[0];

    public void Initialize(
        PerkSystemManager systemManager,
        PerkBarDefinition barDefinition)
    {
        manager = systemManager;

        if (barDefinition != null && barImage != null)
        {
            barImage.sprite = barDefinition.BarSprite;
            barImage.enabled = barImage.sprite != null;
        }
    }

    public void SetOffers(PerkDefinition[] offers)
    {
        int buttonCount = offerButtons != null ? offerButtons.Length : 0;

        CurrentOffers = new PerkDefinition[buttonCount];

        for (int i = 0; i < buttonCount; i++)
        {
            PerkDefinition perk = null;

            if (offers != null && i < offers.Length)
                perk = offers[i];

            CurrentOffers[i] = perk;

            if (offerButtons[i] == null)
                continue;

            if (perk != null)
                offerButtons[i].SetOffer(manager, perk);
            else
                offerButtons[i].ClearOffer();
        }
    }

    public void RefreshOfferButtons()
    {
        if (offerButtons == null)
            return;

        foreach (PerkOfferButton offerButton in offerButtons)
        {
            if (offerButton != null)
                offerButton.Refresh();
        }
    }
}