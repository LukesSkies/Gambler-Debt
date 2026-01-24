using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class IntroController : MonoBehaviour
{
    [Header("Dialogue")]
    public string[] dialogueLines;
    public TypewriterText typewriter;

    [Header("UI")]
    public CanvasGroup choiceGroup;
    public Button yesButtonLeft;
    public Button yesButtonRight;

    [Header("Animation")]
    public Animator introAnimator;

    [Header("Scene")]
    public string gameSceneName = "GameScene";

    private int lineIndex = 0;
    private bool choicesShown = false;

    void Awake()
    {
        if (introAnimator != null)
            introAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    void Start()
    {
        SetChoicesVisible(false);

        // Force animation to start immediately
        if (introAnimator != null)
        {
            introAnimator.Rebind();
            introAnimator.Update(0f);
            introAnimator.Play("Intro", 0, 0f);
        }

        ShowLine(0);
    }

    void Update()
    {
        bool pressed =
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        if (!pressed || choicesShown)
            return;

        if (typewriter != null && typewriter.IsTyping)
        {
            typewriter.Skip();
            return;
        }

        AdvanceDialogue();
    }

    void ShowLine(int index)
    {
        lineIndex = Mathf.Clamp(index, 0, dialogueLines.Length - 1);

        if (typewriter != null)
            typewriter.Play(dialogueLines[lineIndex]);
    }

    void AdvanceDialogue()
    {
        if (lineIndex < dialogueLines.Length - 1)
        {
            ShowLine(lineIndex + 1);
        }
        else
        {
            ShowChoices();
        }
    }

    void ShowChoices()
    {
        choicesShown = true;
        SetChoicesVisible(true);

        yesButtonLeft.onClick.RemoveAllListeners();
        yesButtonRight.onClick.RemoveAllListeners();

        yesButtonLeft.onClick.AddListener(LoadGame);
        yesButtonRight.onClick.AddListener(LoadGame);

        yesButtonLeft.Select();
    }

    void LoadGame()
    {
        StartCoroutine(LoadGameRoutine());
    }

    IEnumerator LoadGameRoutine()
    {
        if (introAnimator != null)
            introAnimator.Play("FadeOut", 0, 0f);

        yield return new WaitForSecondsRealtime(1f);

        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    void SetChoicesVisible(bool show)
    {
        choiceGroup.alpha = show ? 1f : 0f;
        choiceGroup.interactable = show;
        choiceGroup.blocksRaycasts = show;
    }
}
