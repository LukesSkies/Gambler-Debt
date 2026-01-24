using UnityEngine;

public static class GameSettings
{
    // Mouse
    private const string MouseXKey = "MouseSensitivityX";
    private const string MouseYKey = "MouseSensitivityY";
    private const string InvertMouseYKey = "InvertMouseY";

    // Controller
    private const string ControllerXKey = "ControllerSensitivityX";
    private const string ControllerYKey = "ControllerSensitivityY";

    public static float MouseSensitivityX
    {
        get => PlayerPrefs.GetFloat(MouseXKey, 1.5f);
        set { PlayerPrefs.SetFloat(MouseXKey, value); PlayerPrefs.Save(); }
    }

    public static float MouseSensitivityY
    {
        get => PlayerPrefs.GetFloat(MouseYKey, 1.5f);
        set { PlayerPrefs.SetFloat(MouseYKey, value); PlayerPrefs.Save(); }
    }

    public static bool InvertMouseY
    {
        get => PlayerPrefs.GetInt(InvertMouseYKey, 0) == 1;
        set { PlayerPrefs.SetInt(InvertMouseYKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static float ControllerSensitivityX
    {
        get => PlayerPrefs.GetFloat(ControllerXKey, 200f);
        set { PlayerPrefs.SetFloat(ControllerXKey, value); PlayerPrefs.Save(); }
    }

    public static float ControllerSensitivityY
    {
        get => PlayerPrefs.GetFloat(ControllerYKey, 200f);
        set { PlayerPrefs.SetFloat(ControllerYKey, value); PlayerPrefs.Save(); }
    }
}
