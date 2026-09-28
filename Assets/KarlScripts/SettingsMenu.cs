using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private Transform _gameplayOptions;

    private TextMeshProUGUI _playerFieldOfViewText;
    private TextMeshProUGUI _mouseSensitivityText;
    private TextMeshProUGUI _mouseAimSensitivityText;
    private TextMeshProUGUI _controllerSensitivityText;
    private TextMeshProUGUI _controllerAimSensitivityText;

    private Slider _playerFieldOfView;
    private Slider _mouseSensitivity;
    private Slider _mouseAimSensitivity;
    private Slider _controllerSensitivity;
    private Slider _controllerAimSensitivity;

    private Camera _mainCamera;
    private PlayerCamera _playerCamera;

    void Start()
    {
        FindGameObjects();
        LoadSettings();
    }

    private void FindGameObjects()
    {
        _mainCamera = Camera.main;
        _playerCamera = _mainCamera.transform.parent.transform.parent.GetComponent<PlayerCamera>();

        _gameplayOptions = transform.Find("GameplayOptions").transform.Find("Viewport").transform.Find("Content");

        _playerFieldOfView = _gameplayOptions.Find("FieldOfView").GetComponent<Slider>();
        _mouseSensitivity = _gameplayOptions.Find("MouseSensitivity").GetComponent<Slider>();
        _mouseAimSensitivity = _gameplayOptions.Find("MouseAimSensitivity").GetComponent<Slider>();
        _controllerSensitivity = _gameplayOptions.Find("ControllerSensitivity").GetComponent<Slider>();
        _controllerAimSensitivity = _gameplayOptions.Find("ControllerAimSensitivity").GetComponent<Slider>();

        _playerFieldOfViewText = _playerFieldOfView.transform.Find("Value").GetComponent<TextMeshProUGUI>();
        _mouseSensitivityText = _mouseSensitivity.transform.Find("Value").GetComponent<TextMeshProUGUI>();
        _mouseAimSensitivityText = _mouseAimSensitivity.transform.Find("Value").GetComponent<TextMeshProUGUI>();
        _controllerSensitivityText = _controllerSensitivity.transform.Find("Value").GetComponent<TextMeshProUGUI>();
        _controllerAimSensitivityText = _controllerAimSensitivity.transform.Find("Value").GetComponent<TextMeshProUGUI>();
    }

    private void LoadSettings()
    {
        _playerFieldOfView.value = PlayerPrefs.GetFloat("PlayerFieldOfView", 80);
        _mouseSensitivity.value = PlayerPrefs.GetFloat("PlayerMouseSensitivity", 10);
        _mouseAimSensitivity.value = PlayerPrefs.GetFloat("PlayerMouseAimSensitivity", 6);
        _controllerSensitivity.value = PlayerPrefs.GetFloat("PlayerControllerSensitivity", 400);
        _controllerAimSensitivity.value = PlayerPrefs.GetFloat("PlayerControllerAimSensitivity", 200);

        _playerFieldOfViewText.text = _playerFieldOfView.value.ToString();
        _mouseSensitivityText.text = _mouseSensitivity.value.ToString();
        _mouseAimSensitivityText.text = _mouseAimSensitivity.value.ToString();
        _controllerSensitivityText.text = _controllerSensitivity.value.ToString();
        _controllerAimSensitivityText.text = _controllerAimSensitivity.value.ToString();
    }

    public void ApplySettings()
    {
        PlayerPrefs.SetFloat("PlayerFieldOfView", _playerFieldOfView.value);
        PlayerPrefs.SetFloat("PlayerMouseSensitivity", _mouseSensitivity.value);
        PlayerPrefs.SetFloat("PlayerMouseAimSensitivity", _mouseAimSensitivity.value);
        PlayerPrefs.SetFloat("PlayerControllerSensitivity", _controllerSensitivity.value);
        PlayerPrefs.SetFloat("PlayerControllerAimSensitivity", _controllerAimSensitivity.value);

        _playerCamera.DefaultFOV = PlayerPrefs.GetFloat("PlayerFieldOfView");
        _playerCamera.SprintFOV = _playerCamera.DefaultFOV + _playerCamera.FOVSprintSpeedChange;
        _playerCamera.AimingFOV = _playerCamera.DefaultFOV + _playerCamera.FOVAimingChange;

        Debug.Log(HorizontalToVertical(PlayerPrefs.GetFloat("PlayerFieldOfView")));;

        _playerCamera.MouseSensitivity = PlayerPrefs.GetFloat("PlayerMouseSensitivity");
        _playerCamera.MouseAimSensitivity = PlayerPrefs.GetFloat("PlayerMouseAimSensitivity");
        _playerCamera.ControllerSensitivity = PlayerPrefs.GetFloat("PlayerControllerSensitivity");
        _playerCamera.ControllerAimSensitivity = PlayerPrefs.GetFloat("PlayerControllerAimSensitivity");
    }

    private float HorizontalToVertical(float fov)
    {
        float aspectRatio = (float)Screen.width / Screen.height;
        float verticalFOV = 2f * Mathf.Atan(Mathf.Tan(fov * Mathf.Deg2Rad / 2f) / aspectRatio) * Mathf.Rad2Deg;
        return verticalFOV;
    }
}
