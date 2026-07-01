using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private Transform _gameplayOptions;

    private TextMeshProUGUI _playerFieldOfViewText;
    private TextMeshProUGUI _gunFieldOfViewText;

    private Slider _playerFieldOfView;
    private Slider _gunFieldOfView;
    private Slider _mouseSensitivity;
    private Slider _controllerSensitivity;

    private Camera _mainCamera;
    private PlayerCamera _playerCamera;
    private Camera _gunCamera;

    void Start()
    {
        FindGameObjects();
        LoadSettings();
    }

    private void Update()
    {
        UpdateText();
    }

    private void FindGameObjects()
    {
        _mainCamera = Camera.main;
        _gunCamera = _mainCamera.transform.parent.Find("GunCamera").GetComponent<Camera>();
        _playerCamera = _mainCamera.transform.parent.transform.parent.GetComponent<PlayerCamera>();

        _gameplayOptions = transform.Find("GameplayOptions").transform.Find("Viewport").transform.Find("Content");

        _playerFieldOfView = _gameplayOptions.Find("PlayerFieldOfView").GetComponent<Slider>();
        _gunFieldOfView = _gameplayOptions.Find("GunFieldOfView").GetComponent<Slider>();

        _playerFieldOfViewText = _playerFieldOfView.transform.Find("Value").GetComponent<TextMeshProUGUI>();
        _gunFieldOfViewText = _gunFieldOfView.transform.Find("Value").GetComponent<TextMeshProUGUI>();
    }

    private void LoadSettings()
    {
        _playerFieldOfView.value = PlayerPrefs.GetFloat("PlayerFieldOfView", 80);
        _gunFieldOfView.value = PlayerPrefs.GetFloat("GunFieldOfView", 90);
    }

    public void ApplySettings()
    {
        PlayerPrefs.SetFloat("PlayerFieldOfView", _playerFieldOfView.value);
        PlayerPrefs.SetFloat("GunFieldOfView", _gunFieldOfView.value);

        _playerCamera.DefaultFOV = PlayerPrefs.GetFloat("PlayerFieldOfView");
        _playerCamera.DefaultGunFOV = PlayerPrefs.GetFloat("GunFieldOfView");
        _playerCamera.SprintFOV = _playerCamera.DefaultFOV + _playerCamera.FOVSprintSpeedChange;
        _playerCamera.AimingFOV = _playerCamera.DefaultFOV + _playerCamera.FOVAimingChange;
        _playerCamera.AimingGunFOV = _playerCamera.DefaultGunFOV + _playerCamera.FOVAimingChange;

        Debug.Log(HorizontalToVertical(PlayerPrefs.GetFloat("PlayerFieldOfView")));
        Debug.Log(HorizontalToVertical(PlayerPrefs.GetFloat("GunFieldOfView")));
    }

    public void UpdateText()
    {
        _playerFieldOfViewText.text = _playerFieldOfView.value.ToString();
        _gunFieldOfViewText.text = _gunFieldOfView.value.ToString();
    }

    private float HorizontalToVertical(float fov)
    {
        float aspectRatio = (float)Screen.width / Screen.height;
        float verticalFOV = 2f * Mathf.Atan(Mathf.Tan(fov * Mathf.Deg2Rad / 2f) / aspectRatio) * Mathf.Rad2Deg;
        return verticalFOV;
    }
}
