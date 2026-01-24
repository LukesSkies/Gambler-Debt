using UnityEngine;

[CreateAssetMenu(menuName = "Intro/Dialogue Sequence")]
public class IntroDialogueData : ScriptableObject
{
    [TextArea(3, 6)]
    public string[] lines;
}
