using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PerkInspectUI : MonoBehaviour
{
    public static PerkInspectUI Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private Canvas canvas;

    [Header("Inspect Popup")]
    [SerializeField] private RectTransform popupRect;
    [SerializeField] private Image popupImage;

    [Header("Optional Text")]
    [SerializeField] private TMP_Text perkNameText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text cooldownText;

    [Header("Positioning")]
    [Tooltip("Distance between the perk and the inspect popup, in screen pixels.")]
    [SerializeField] private float screenGap = 15f;

    private RectTransform positioningRect;
    private RectTransform currentTarget;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("There is more than one PerkInspectUI in the scene.");
            enabled = false;
            return;
        }

        Instance = this;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (popupRect == null)
        {
            Debug.LogError("PerkInspectUI is missing its Popup Rect Transform.");
            enabled = false;
            return;
        }

        positioningRect = popupRect.parent as RectTransform;

        if (positioningRect == null)
        {
            Debug.LogError("The inspect popup must have a RectTransform parent.");
            enabled = false;
            return;
        }

        if (popupImage == null)
            popupImage = popupRect.GetComponent<Image>();

        if (popupImage == null)
        {
            Debug.LogError("The inspect popup needs an Image component.");
            enabled = false;
            return;
        }

        // The popup uses a centred anchor so it can be positioned
        // using screen coordinates converted into canvas coordinates.
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);

        // Prevent the popup from intercepting pointer events.
        popupImage.raycastTarget = false;

        CanvasGroup canvasGroup = popupRect.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = popupRect.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Hide the popup initially.
        popupRect.gameObject.SetActive(false);
    }

    // SHOW INSPECT

    public void Show(
        PerkDefinition perk,
        int tier,
        RectTransform target)
    {
        if (!enabled || perk == null || target == null)
            return;

        if (canvas == null)
        {
            Debug.LogError("PerkInspectUI could not find its Canvas.");
            return;
        }

        if (perk.InspectSprite == null)
        {
            Debug.LogWarning(
                perk.PerkName + " does not have an Inspect Sprite assigned."
            );

            HideAll();
            return;
        }

        if (popupImage == null)
            return;

        // Remember which UI element is currently being inspected.
        currentTarget = target;

        tier = Mathf.Clamp(tier, 1, 3);

        // Set the unique artwork belonging to this perk.
        popupImage.sprite = perk.InspectSprite;
        popupImage.preserveAspect = true;
        popupImage.enabled = true;

        // Calculate the image's native size.
        // This preserves the image's original proportions.
        popupImage.SetNativeSize();

        // Apply this perk's individual scale.
        Vector2 nativeSize = popupImage.rectTransform.sizeDelta;

        Vector2 scaledSize = nativeSize * perk.InspectScale;

        popupRect.sizeDelta = scaledSize;

        // The panel image should fill the popup.
        if (popupImage.rectTransform != popupRect)
            popupImage.rectTransform.sizeDelta = scaledSize;

        // Update optional text fields.
        if (perkNameText != null)
            perkNameText.text = perk.PerkName;

        if (tierText != null)
            tierText.text = "TIER " + tier;

        if (descriptionText != null)
            descriptionText.text = perk.GetTierDescription(tier);

        if (cooldownText != null)
        {
            cooldownText.text = perk.CooldownDuration > 0f
                ? "COOLDOWN: " + perk.CooldownDuration.ToString("0.##") + "s"
                : "NO COOLDOWN";
        }

        // Display the popup.
        popupRect.gameObject.SetActive(true);

        // Make sure it renders above other UI.
        popupRect.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();

        // Position it next to the hovered perk.
        PositionPopup(target);
    }

    // POSITIONING
    private void PositionPopup(RectTransform target)
    {
        if (target == null || popupRect == null)
            return;

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        // Get the screen-space bounds of the hovered perk.
        Vector3[] targetCorners = new Vector3[4];

        target.GetWorldCorners(targetCorners);

        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(
            uiCamera,
            targetCorners[0]
        );

        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(
            uiCamera,
            targetCorners[2]
        );

        float targetLeft = bottomLeft.x;
        float targetRight = topRight.x;

        float targetCenterY = (bottomLeft.y + topRight.y) * 0.5f;

        // Convert the popup's canvas dimensions into screen pixels.
        float scaleFactor = Mathf.Max(0.01f, canvas.scaleFactor);

        float popupWidth = popupRect.rect.width * scaleFactor;
        float popupHeight = popupRect.rect.height * scaleFactor;

        float rightSpace = Screen.width - targetRight;
        float leftSpace = targetLeft;

        // Prefer the right side if it has enough room.
        // Otherwise, use whichever side has more space.
        bool placeOnRight =
            rightSpace >= popupWidth + screenGap ||
            rightSpace >= leftSpace;

        float screenX;

        if (placeOnRight)
        {
            popupRect.pivot = new Vector2(0f, 0.5f);

            screenX = targetRight + screenGap;

            screenX = Mathf.Clamp(
                screenX,
                0f,
                Mathf.Max(0f, Screen.width - popupWidth)
            );
        }
        else
        {
            popupRect.pivot = new Vector2(1f, 0.5f);

            screenX = targetLeft - screenGap;

            screenX = Mathf.Clamp(
                screenX,
                Mathf.Min(popupWidth, Screen.width),
                Screen.width
            );
        }

        // Keep the popup inside the top and bottom of the screen.
        float halfPopupHeight = popupHeight * 0.5f;

        float screenY;

        if (popupHeight >= Screen.height)
        {
            screenY = Screen.height * 0.5f;
        }
        else
        {
            screenY = Mathf.Clamp(
                targetCenterY,
                halfPopupHeight,
                Screen.height - halfPopupHeight
            );
        }

        // Convert screen coordinates into the popup parent's local space.
        Vector2 screenPosition = new Vector2(screenX, screenY);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            positioningRect,
            screenPosition,
            uiCamera,
            out Vector2 localPosition))
        {
            popupRect.anchoredPosition = localPosition;
        }
    }

    // HIDE INSPECT
    public void Hide(RectTransform target)
    {
        // Only hide when the element leaving the pointer
        // is the element currently being inspected.
        if (currentTarget != target)
            return;

        HideAll();
    }

    public void HideAll()
    {
        currentTarget = null;

        if (popupRect != null)
            popupRect.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}