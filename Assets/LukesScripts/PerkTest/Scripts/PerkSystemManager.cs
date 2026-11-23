
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PerkSystemManager : MonoBehaviour
{
    public static PerkSystemManager Instance { get; private set; }

    [Header("Starting Values")]
    [SerializeField] private int startingPoints = 10000;
    [SerializeField] private int startingRound = 1;

    [Header("Round Settings")]
    [Min(1)]
    [SerializeField] private int roundsBetweenOfferRefresh = 5;

    [Header("Round Rewards")]
    [Min(0)]
    [SerializeField] private int pointsPerRound = 2500;

    [Header("Perk Slot Settings")]
    [Min(1)]
    [SerializeField] private int maxPerkSlots = 6;

    [Header("Bar Data")]
    [Tooltip("Order: Beer, Cocktail, Wine, Vodka.")]
    [SerializeField]
    private PerkBarDefinition[] barDefinitions =
        new PerkBarDefinition[4];

    [Header("Bar UI")]
    [Tooltip("Each UI element must correspond to the same index in Bar Data.")]
    [SerializeField]
    private PerkBarUI[] barPanels =
        new PerkBarUI[4];

    [Header("Equipped Perk Slots")]
    [SerializeField]
    private PerkSlotUI[] equippedSlots =
        new PerkSlotUI[6];

    [Header("General UI")]
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private Button advanceRoundButton;

    private int points;
    private int currentRound;

    // Tracks which perks have already been purchased
    // during the current shop cycle.
    private readonly HashSet<PerkDefinition> purchasedPerksThisShopCycle =
        new HashSet<PerkDefinition>();

    public int Points => points;
    public int CurrentRound => currentRound;

    private const int OffersPerBar = 3;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("There is more than one PerkSystemManager in the scene.");
            enabled = false;
            return;
        }

        Instance = this;

        points = Mathf.Max(0, startingPoints);
        currentRound = Mathf.Max(1, startingRound);
    }

    private void Start()
    {
        if (advanceRoundButton != null)
            advanceRoundButton.onClick.AddListener(AdvanceRound);

        InitializeBars();

        RefreshAllOffers();
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (advanceRoundButton != null)
            advanceRoundButton.onClick.RemoveListener(AdvanceRound);

        if (Instance == this)
            Instance = null;
    }

    // --------------------------------------------------
    // INITIALIZATION
    // --------------------------------------------------

    private void InitializeBars()
    {
        if (barDefinitions == null || barPanels == null)
            return;

        if (barDefinitions.Length != barPanels.Length)
        {
            Debug.LogWarning(
                "Your Bar Definitions and Bar Panels arrays should have matching lengths."
            );
        }

        int count = Mathf.Min(barDefinitions.Length, barPanels.Length);

        for (int i = 0; i < count; i++)
        {
            if (barDefinitions[i] == null)
            {
                Debug.LogWarning("A Perk Bar Definition is missing at index " + i);
                continue;
            }

            if (barPanels[i] == null)
            {
                Debug.LogWarning("A Perk Bar UI is missing at index " + i);
                continue;
            }

            barPanels[i].Initialize(this, barDefinitions[i]);

            if (barDefinitions[i].Perks == null ||
                barDefinitions[i].Perks.Length < OffersPerBar)
            {
                Debug.LogWarning(
                    barDefinitions[i].BarName +
                    " needs at least three assigned perks."
                );
            }
        }
    }

    // --------------------------------------------------
    // PURCHASE COSTS
    // --------------------------------------------------

    public static int GetTierCost(int tier)
    {
        switch (tier)
        {
            case 1:
                return 2500;

            case 2:
                return 5000;

            case 3:
                return 7500;

            default:
                return 0;
        }
    }

    // --------------------------------------------------
    // PERK OWNERSHIP
    // --------------------------------------------------

    public int GetOwnedTier(PerkDefinition perk)
    {
        if (perk == null)
            return 0;

        PerkSlotUI slot = FindSlotForPerk(perk);

        if (slot == null)
            return 0;

        return slot.CurrentTier;
    }

    private PerkSlotUI FindSlotForPerk(PerkDefinition perk)
    {
        if (perk == null || equippedSlots == null)
            return null;

        int slotCount = GetUsableSlotCount();

        for (int i = 0; i < slotCount; i++)
        {
            PerkSlotUI slot = equippedSlots[i];

            if (slot == null)
                continue;

            if (slot.CurrentPerk == perk)
                return slot;
        }

        return null;
    }

    private PerkSlotUI FindEmptySlot()
    {
        if (equippedSlots == null)
            return null;

        int slotCount = GetUsableSlotCount();

        for (int i = 0; i < slotCount; i++)
        {
            PerkSlotUI slot = equippedSlots[i];

            if (slot != null && slot.IsEmpty)
                return slot;
        }

        return null;
    }

    private int GetUsableSlotCount()
    {
        if (equippedSlots == null)
            return 0;

        return Mathf.Min(maxPerkSlots, equippedSlots.Length);
    }

    // SHOP PURCHASE LIMIT
    public bool HasPurchasedThisShopCycle(PerkDefinition perk)
    {
        if (perk == null)
            return false;

        return purchasedPerksThisShopCycle.Contains(perk);
    }

    // PURCHASE AND UPGRADE
    public bool TryPurchase(PerkDefinition perk)
    {
        if (perk == null)
            return false;

        // Prevent purchasing the same perk more than once
        // before the next shop refresh.
        if (HasPurchasedThisShopCycle(perk))
            return false;

        // Check whether the player already owns this perk.
        PerkSlotUI existingSlot = FindSlotForPerk(perk);

        int currentTier = existingSlot != null
            ? existingSlot.CurrentTier
            : 0;

        // Prevent upgrading beyond Tier 3.
        if (currentTier >= 3)
            return false;

        int nextTier = currentTier + 1;
        int cost = GetTierCost(nextTier);

        // Check points.
        if (points < cost)
            return false;

        // Upgrades use their existing slot.
        // New perks need an empty slot.
        PerkSlotUI targetSlot = existingSlot;

        if (targetSlot == null)
            targetSlot = FindEmptySlot();

        if (targetSlot == null)
            return false;

        // Complete the transaction.
        points -= cost;

        // Update perk images and cooldown.
        targetSlot.SetPerk(perk, nextTier);

        // Mark this perk as purchased until the next shop refresh.
        purchasedPerksThisShopCycle.Add(perk);

        UpdateUI();

        return true;
    }

    // POINTS
    public void AddPoints(int amount)
    {
        points = Mathf.Max(0, points + amount);

        UpdateUI();
    }

    // ROUND PROGRESSION
    public void AdvanceRound()
    {
        // Advance the round.
        currentRound++;

        // Award the player their round reward.
        points = Mathf.Max(0, points + pointsPerRound);

        // Determine whether the shop should refresh.
        bool shouldRefresh =
            roundsBetweenOfferRefresh > 0 &&
            currentRound % roundsBetweenOfferRefresh == 0;

        if (shouldRefresh)
            RefreshAllOffers();

        UpdateUI();
    }

    // OFFER REFRESHING
    public void RefreshAllOffers()
    {
        if (barDefinitions == null || barPanels == null)
            return;

        // Reset all purchase restrictions.
        // Players can now buy each perk once again.
        purchasedPerksThisShopCycle.Clear();

        int count = Mathf.Min(barDefinitions.Length, barPanels.Length);

        for (int i = 0; i < count; i++)
        {
            PerkBarDefinition barDefinition = barDefinitions[i];
            PerkBarUI barUI = barPanels[i];

            if (barDefinition == null || barUI == null)
                continue;

            PerkDefinition[] newOffers = ChooseThreePerks(
                barDefinition.Perks,
                barUI.CurrentOffers
            );

            barUI.SetOffers(newOffers);
        }
    }

    private PerkDefinition[] ChooseThreePerks(
        PerkDefinition[] allPerks,
        PerkDefinition[] previousOffers)
    {
        List<PerkDefinition> availablePerks =
            new List<PerkDefinition>();

        if (allPerks == null)
            return new PerkDefinition[0];

        // Remove empty entries and duplicates.
        foreach (PerkDefinition perk in allPerks)
        {
            if (perk != null && !availablePerks.Contains(perk))
                availablePerks.Add(perk);
        }

        if (availablePerks.Count == 0)
            return new PerkDefinition[0];

        int offerCount = Mathf.Min(
            OffersPerBar,
            availablePerks.Count
        );

        PerkDefinition[] selectedPerks =
            new PerkDefinition[offerCount];

        // Try different selections so the same offers
        // are not shown again immediately when alternatives exist.
        for (int attempt = 0; attempt < 30; attempt++)
        {
            List<PerkDefinition> shuffled =
                new List<PerkDefinition>(availablePerks);

            // Fisher-Yates shuffle.
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);

                PerkDefinition temporary = shuffled[i];

                shuffled[i] = shuffled[randomIndex];
                shuffled[randomIndex] = temporary;
            }

            for (int i = 0; i < offerCount; i++)
                selectedPerks[i] = shuffled[i];

            if (!SameOfferSelection(selectedPerks, previousOffers))
                return selectedPerks;
        }

        // Return the last selection if a different combination
        // could not be found.
        return selectedPerks;
    }

    private bool SameOfferSelection(
        PerkDefinition[] first,
        PerkDefinition[] second)
    {
        if (first == null || second == null)
            return false;

        if (first.Length != second.Length)
            return false;

        for (int i = 0; i < first.Length; i++)
        {
            bool found = false;

            for (int j = 0; j < second.Length; j++)
            {
                if (first[i] == second[j])
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                return false;
        }

        return true;
    }

    // UI UPDATES
    private void UpdateUI()
    {
        if (pointsText != null)
            pointsText.text = points.ToString("N0");

        if (roundText != null)
            roundText.text = currentRound.ToString();

        // Refresh all offer buttons so they display the
        // correct next tier, cost, and purchase availability.
        if (barPanels != null)
        {
            foreach (PerkBarUI bar in barPanels)
            {
                if (bar != null)
                    bar.RefreshOfferButtons();
            }
        }
    }
}