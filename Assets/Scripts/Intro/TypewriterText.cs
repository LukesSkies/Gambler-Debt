using TMPro;
using UnityEngine;
using System.Collections;

public class TypewriterText : MonoBehaviour
{
    public TMP_Text text;
    public float charDelay = 0.035f;

    private Coroutine routine;
    private bool skip;

    public bool IsTyping { get; private set; }

    public void Play(string message)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(TypeRoutine(message));
    }

    IEnumerator TypeRoutine(string message)
    {
        skip = false;
        IsTyping = true;
        text.text = "";

        foreach (char c in message)
        {
            if (skip)
            {
                text.text = message;
                IsTyping = false;
                yield break;
            }

            text.text += c;
            yield return new WaitForSecondsRealtime(charDelay);
        }

        IsTyping = false;
    }

    public void Skip()
    {
        skip = true;
    }
}
