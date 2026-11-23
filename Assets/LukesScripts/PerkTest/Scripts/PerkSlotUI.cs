using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PerkSlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Perk Images")]
    [SerializeField] private Image perkImage;
    [SerializeField] private Image cooldownImage;

    [Header("Cooldown")]
    [SerializeField] private PerkCooldown perkCooldown;

    [Header("Starting State")]
    [SerializeField] private bool startEmpty = true;

    public PerkDefinition CurrentPerk { get; private set; }

    public int CurrentTier { get; private set; }

    public bool IsEmpty => CurrentPerk == null;

    private RectTransform HoverRect =>
        transform as RectTransform;

    private void Awake()
    {
        if (startEmpty)
            ClearSlot();
    }

    // PERK SETUP

    public void SetPerk(PerkDefinition perk, int tier)
    {
        if (perk == null)
            return;

        CurrentPerk = perk;
        CurrentTier = Mathf.Clamp(tier, 1, 3);

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (CurrentPerk == null)
        {
            ClearSlot();
            return;
        }

        Sprite tierSprite = CurrentPerk.GetTierSprite(CurrentTier);

        if (perkImage != null)
        {
            perkImage.sprite = tierSprite;
            perkImage.enabled = tierSprite != null;
        }

        if (perkCooldown != null)
        {
            perkCooldown.Configure(
                CurrentPerk,
                CurrentTier,
                cooldownImage
            );
        }
    }

    public void ClearSlot()
    {
        if (PerkInspectUI.Instance != null)
            PerkInspectUI.Instance.Hide(HoverRect);

        CurrentPerk = null;
        CurrentTier = 0;

        if (perkImage != null)
        {
            perkImage.sprite = null;
            perkImage.enabled = false;
        }

        if (perkCooldown != null)
        {
            perkCooldown.Configure(null, 0, cooldownImage);
        }
        else if (cooldownImage != null)
        {
            cooldownImage.fillAmount = 1f;
            cooldownImage.enabled = false;
        }
    }

    // MOUSE HOVER
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsEmpty)
            return;

        if (PerkInspectUI.Instance == null)
            return;

        PerkInspectUI.Instance.Show(
            CurrentPerk,
            CurrentTier,
            HoverRect
        );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (PerkInspectUI.Instance != null)
            PerkInspectUI.Instance.Hide(HoverRect);
    }

    // ACTIVATE PERK
    public void ActivatePerk()
    {
        if (IsEmpty)
            return;

        if (perkCooldown != null)
            perkCooldown.ActivatePerk();
    }
}