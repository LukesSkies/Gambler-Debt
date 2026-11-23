
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[System.Serializable]
public class PerkInspectEntry
{
    [Header("Perk Information")]
    public string title;

    [TextArea(3, 8)]
    public string description;

    [Header("3D Model")]
    public GameObject modelPrefab;
}

public class PerkInspectCarousel : MonoBehaviour
{
    [Header("PERK DATA")]
    [SerializeField] private List<PerkInspectEntry> perks = new List<PerkInspectEntry>();
    [SerializeField] private int startingIndex = 0;

    [Header("MODEL POSITIONS")]
    [SerializeField] private Transform topPosition;
    [SerializeField] private Transform middlePosition;
    [SerializeField] private Transform bottomPosition;
    [SerializeField] private Transform modelParent;

    [Header("MODEL SCALE")]
    [SerializeField] private float topScaleMultiplier = 0.7f;
    [SerializeField] private float middleScaleMultiplier = 1f;
    [SerializeField] private float bottomScaleMultiplier = 0.7f;

    [Header("ANIMATION")]
    [SerializeField] private float transitionDuration = 0.4f;
    [SerializeField] private float textFadeDuration = 0.12f;

    [Header("DESCRIPTION UI")]
    [SerializeField] private CanvasGroup descriptionCanvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text counterText;

    [Header("KEYBOARD INPUT")]
    [SerializeField] private bool enableKeyboard = true;

    [Header("MOUSE INPUT")]
    [SerializeField] private bool enableMouseWheel = true;
    [SerializeField] private RectTransform scrollInputArea;
    [SerializeField] private float mouseWheelThreshold = 0.01f;

    [Header("UI BUTTONS")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    // Only one model is stored at a time.

    private GameObject currentModel;
    private Vector3 currentModelBaseScale;

    private int currentIndex;

    private bool isInitialised;

    // Stores every input while an animation is playing.
    // Positive = next, negative = previous.

    private readonly Queue<int> inputQueue = new Queue<int>();

    private Coroutine queueCoroutine;

    // INITIALISATION

    private void Start()
    {
        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        currentIndex = startingIndex;

        currentModel = CreateModel(
            perks[currentIndex],
            middlePosition,
            middleScaleMultiplier,
            out currentModelBaseScale
        );

        if (currentModel == null)
        {
            enabled = false;
            return;
        }

        UpdateDescription();

        if (descriptionCanvasGroup != null)
            descriptionCanvasGroup.alpha = 1f;

        if (previousButton != null)
            previousButton.onClick.AddListener(PreviousPerk);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextPerk);

        UpdateButtonStates();

        isInitialised = true;
    }

    private void OnDestroy()
    {
        if (previousButton != null)
            previousButton.onClick.RemoveListener(PreviousPerk);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(NextPerk);
    }

    private bool ValidateSetup()
    {
        if (perks == null || perks.Count == 0)
        {
            Debug.LogError("Perk Carousel: No perks assigned.");
            return false;
        }

        if (topPosition == null ||
            middlePosition == null ||
            bottomPosition == null)
        {
            Debug.LogError("Perk Carousel: Assign Top, Middle and Bottom.");
            return false;
        }

        if (titleText == null || descriptionText == null)
        {
            Debug.LogError("Perk Carousel: Title or Description text is missing.");
            return false;
        }

        startingIndex = Mathf.Clamp(
            startingIndex,
            0,
            perks.Count - 1
        );

        // Validate every perk so the carousel never reaches
        // an empty model slot when looping around.

        for (int i = 0; i < perks.Count; i++)
        {
            if (perks[i] == null || perks[i].modelPrefab == null)
            {
                Debug.LogError(
                    $"Perk Carousel: Perk {i + 1} is missing its model prefab."
                );

                return false;
            }
        }

        return true;
    }

    // INPUT
    private void Update()
    {
        if (!isInitialised)
            return;

        if (enableKeyboard)
            HandleKeyboardInput();

        if (enableMouseWheel)
            HandleMouseWheelInput();
    }

    private void HandleKeyboardInput()
    {
#if ENABLE_INPUT_SYSTEM

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
            PreviousPerk();

        if (Keyboard.current.eKey.wasPressedThisFrame)
            NextPerk();

#elif ENABLE_LEGACY_INPUT_MANAGER

        if (Input.GetKeyDown(KeyCode.Q))
            PreviousPerk();

        if (Input.GetKeyDown(KeyCode.E))
            NextPerk();

#endif
    }

    private void HandleMouseWheelInput()
    {
        float scroll = 0f;
        Vector2 mousePosition = Vector2.zero;

#if ENABLE_INPUT_SYSTEM

        if (Mouse.current == null)
            return;

        scroll = Mouse.current.scroll.ReadValue().y;
        mousePosition = Mouse.current.position.ReadValue();

#elif ENABLE_LEGACY_INPUT_MANAGER

        scroll = Input.mouseScrollDelta.y;
        mousePosition = Input.mousePosition;

#endif

        if (!IsPointerInsideScrollArea(mousePosition))
            return;

        if (scroll > mouseWheelThreshold)
            PreviousPerk();
        else if (scroll < -mouseWheelThreshold)
            NextPerk();
    }

    private bool IsPointerInsideScrollArea(Vector2 pointerPosition)
    {
        if (scrollInputArea == null)
            return true;

        Canvas canvas = scrollInputArea.GetComponentInParent<Canvas>();

        Camera eventCamera = null;

        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = canvas.worldCamera;

            if (eventCamera == null)
                eventCamera = Camera.main;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            scrollInputArea,
            pointerPosition,
            eventCamera
        );
    }

    // PUBLIC CONTROLS
    public void PreviousPerk()
    {
        QueueInput(-1);
    }

    public void NextPerk()
    {
        QueueInput(1);
    }

    private void QueueInput(int direction)
    {
        if (!isInitialised || perks.Count <= 1)
            return;

        // Remember the input instead of ignoring it
        // while the model is animating.

        inputQueue.Enqueue(direction);

        // Only one queue processor can run at a time.

        if (queueCoroutine == null)
        {
            queueCoroutine = StartCoroutine(ProcessInputQueue());
        }
    }

    // INPUT QUEUE
    private IEnumerator ProcessInputQueue()
    {
        while (inputQueue.Count > 0)
        {
            int direction = inputQueue.Dequeue();

            int nextIndex = WrapIndex(
                currentIndex + direction
            );

            // Play the requested transition.

            yield return TransitionToPerk(direction, nextIndex);
        }

        // All queued inputs have been processed.

        queueCoroutine = null;

        UpdateButtonStates();
    }

    // CAROUSEL TRANSITION
    private IEnumerator TransitionToPerk(
        int direction,
        int targetIndex
    )
    {
        bool movingNext = direction > 0;

        // E: exit at TOP, enter at BOTTOM.
        // Q: exit at BOTTOM, enter at TOP.

        Transform exitPosition = movingNext
            ? topPosition
            : bottomPosition;

        Transform entryPosition = movingNext
            ? bottomPosition
            : topPosition;

        float exitScale = movingNext
            ? topScaleMultiplier
            : bottomScaleMultiplier;

        float entryScale = movingNext
            ? bottomScaleMultiplier
            : topScaleMultiplier;

        // 1. Move the current model out and fade out old text.
        yield return AnimateModel(
            currentModel,
            currentModelBaseScale,
            exitPosition,
            exitScale,
            0f
        );

        // 2. Destroy the old model completely.
        if (currentModel != null)
        {
            currentModel.SetActive(false);

            Destroy(currentModel);

            currentModel = null;
        }

        // Wait one frame so Unity has processed Destroy.
        // This prevents the outgoing and incoming models
        // from existing together.

        yield return null;

        // 3. Update the selected perk.
        currentIndex = targetIndex;

        UpdateDescription();


        // 4. Spawn the incoming model at TOP or BOTTOM.

        currentModel = CreateModel(
            perks[currentIndex],
            entryPosition,
            entryScale,
            out currentModelBaseScale
        );

        if (currentModel == null)
        {
            if (descriptionCanvasGroup != null)
                descriptionCanvasGroup.alpha = 1f;

            yield break;
        }

        // 5. Animate the new model into MIDDLE.
        yield return AnimateModel(
            currentModel,
            currentModelBaseScale,
            middlePosition,
            middleScaleMultiplier,
            1f
        );

        // Counter and buttons reflect the new selection.

        UpdateButtonStates();
    }

    // MODEL CREATION
    private GameObject CreateModel(
        PerkInspectEntry perk,
        Transform spawnPosition,
        float scaleMultiplier,
        out Vector3 originalScale
    )
    {
        originalScale = Vector3.one;

        if (perk == null || perk.modelPrefab == null)
        {
            Debug.LogError("Perk Carousel: Missing model prefab.");
            return null;
        }

        GameObject instance;

        if (modelParent != null)
        {
            instance = Instantiate(
                perk.modelPrefab,
                modelParent,
                false
            );
        }
        else
        {
            instance = Instantiate(perk.modelPrefab);
        }

        Transform modelTransform = instance.transform;

        originalScale = modelTransform.localScale;

        modelTransform.SetPositionAndRotation(
            spawnPosition.position,
            spawnPosition.rotation
        );

        modelTransform.localScale =
            originalScale * scaleMultiplier;

        return instance;
    }


    // MODEL ANIMATION
    private IEnumerator AnimateModel(
        GameObject model,
        Vector3 baseScale,
        Transform targetPosition,
        float targetScaleMultiplier,
        float targetTextAlpha
    )
    {
        if (model == null)
            yield break;

        Transform modelTransform = model.transform;

        Vector3 startPosition = modelTransform.position;
        Quaternion startRotation = modelTransform.rotation;
        Vector3 startScale = modelTransform.localScale;

        Vector3 endPosition = targetPosition.position;
        Quaternion endRotation = targetPosition.rotation;

        Vector3 endScale = baseScale * targetScaleMultiplier;

        float startAlpha = descriptionCanvasGroup != null
            ? descriptionCanvasGroup.alpha
            : targetTextAlpha;

        if (transitionDuration <= 0f)
        {
            modelTransform.SetPositionAndRotation(
                endPosition,
                endRotation
            );

            modelTransform.localScale = endScale;

            if (descriptionCanvasGroup != null)
                descriptionCanvasGroup.alpha = targetTextAlpha;

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / transitionDuration
            );

            float easedProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            // Position.

            modelTransform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                easedProgress
            );

            // Rotation.

            modelTransform.rotation = Quaternion.Slerp(
                startRotation,
                endRotation,
                easedProgress
            );

            // Scale.

            modelTransform.localScale = Vector3.Lerp(
                startScale,
                endScale,
                easedProgress
            );

            // Text fade.

            if (descriptionCanvasGroup != null)
            {
                float fadeProgress = textFadeDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / textFadeDuration);

                descriptionCanvasGroup.alpha = Mathf.Lerp(
                    startAlpha,
                    targetTextAlpha,
                    fadeProgress
                );
            }

            yield return null;
        }

        modelTransform.SetPositionAndRotation(
            endPosition,
            endRotation
        );

        modelTransform.localScale = endScale;

        if (descriptionCanvasGroup != null)
            descriptionCanvasGroup.alpha = targetTextAlpha;
    }

    // TEXT
    private void UpdateDescription()
    {
        PerkInspectEntry perk = perks[currentIndex];

        titleText.text = perk.title;

        descriptionText.text = perk.description;

        if (counterText != null)
        {
            counterText.text =
                $"{currentIndex + 1:00} / {perks.Count:00}";
        }
    }

    // INFINITE LOOPING
    private int WrapIndex(int index)
    {
        return (index % perks.Count + perks.Count) % perks.Count;
    }

    private void UpdateButtonStates()
    {
        bool canNavigate = perks.Count > 1;

        // Never disable the buttons at the ends.
        // The carousel always loops around.

        if (previousButton != null)
            previousButton.interactable = canNavigate;

        if (nextButton != null)
            nextButton.interactable = canNavigate;
    }
}
