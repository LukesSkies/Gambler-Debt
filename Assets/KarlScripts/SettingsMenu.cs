using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private Transform _gameplayOptions;
    private Transform _graphicOptions;
    
    //Gameplay Settings
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

    //Graphic Settings
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private Toggle _vsyncToggle;
    [SerializeField] private TMP_Dropdown _fullscreenDropdown;
    [SerializeField] private TMP_Dropdown _antiAliasingDropdown;
    [SerializeField] private TMP_Dropdown _shadowsDropdown;
    [SerializeField] private TMP_Dropdown _softShadowQualityDropdown;
    [SerializeField] private TMP_Dropdown _textureQualityDropdown;

    public bool SettingsHasChanged;
    public bool SettingsApplied;

    [SerializeField] private GameObject _unappliedSettingsPanel;

    private Resolution[] _resolutions;
    [SerializeField] private List<string> _resolutionOptions = new List<string>();
    private int _currentResolutionIndex;

    private Camera _mainCamera;
    private PlayerCamera _playerCamera;

    private void Awake()
    {
        FindGameObjects();
    }

    void Start()
    {
        PopulateGraphicSettings();
        LoadSettings();
    }

    private void FindGameObjects()
    {
        _mainCamera = Camera.main;
        _playerCamera = _mainCamera.transform.parent.transform.parent.GetComponent<PlayerCamera>();

        _gameplayOptions = transform.Find("GameplayOptions").transform.Find("Viewport").transform.Find("Content");
        _graphicOptions = transform.Find("GraphicOptions").transform.Find("Viewport").transform.Find("Content");

        //Gameplay Settings
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

        //Graphic Settings
        _resolutionDropdown = _graphicOptions.GetChild(0).GetComponent<TMP_Dropdown>();
        _vsyncToggle = _graphicOptions.GetChild(1).GetComponent<Toggle>();
        _fullscreenDropdown = _graphicOptions.Find("Fullscreen").GetComponent<TMP_Dropdown>();
        _antiAliasingDropdown = _graphicOptions.Find("Anti-Aliasing").GetComponent<TMP_Dropdown>();
        _shadowsDropdown = _graphicOptions.Find("Shadows").GetComponent<TMP_Dropdown>();
        _softShadowQualityDropdown = _graphicOptions.Find("Soft Shadow Quality").GetComponent<TMP_Dropdown>();
        _textureQualityDropdown = _graphicOptions.Find("Texture Quality").GetComponent<TMP_Dropdown>();
    }

    private void PopulateGraphicSettings()
    {
        _resolutions = Screen.resolutions;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            string option = _resolutions[i].width + " X " + _resolutions[i].height;
            _resolutionOptions.Add(option);
            if (_resolutions[i].width == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height)
            {
                _currentResolutionIndex = i;
            }
        }

        _resolutionDropdown.AddOptions(_resolutionOptions);
        _resolutionDropdown.RefreshShownValue();

        List<string> fullscreenOptions = new List<string>();
        fullscreenOptions.Add("Full Screen Windowed");
        fullscreenOptions.Add("Full Screen Exclusive");
        fullscreenOptions.Add("Windowed");

        _fullscreenDropdown.AddOptions(fullscreenOptions);
        _fullscreenDropdown.RefreshShownValue();

        List<string> antiAliasingOptions = new List<string>();
        antiAliasingOptions.Add("0x");
        antiAliasingOptions.Add("2x");
        antiAliasingOptions.Add("4x");
        antiAliasingOptions.Add("8x");

        _antiAliasingDropdown.AddOptions(antiAliasingOptions);
        _antiAliasingDropdown.RefreshShownValue();

        List<string> shadowOptions = new List<string>();
        shadowOptions.Add("No Shadows");
        shadowOptions.Add("Hard Shadows");
        shadowOptions.Add("Soft Shadows");

        _shadowsDropdown.AddOptions(shadowOptions);
        _shadowsDropdown.RefreshShownValue();

        List<string> softShadowOptions = new List<string>();
        softShadowOptions.Add("Low");
        softShadowOptions.Add("Medium");
        softShadowOptions.Add("High");

        _softShadowQualityDropdown.AddOptions(softShadowOptions);
        _softShadowQualityDropdown.RefreshShownValue();

        List<string> textureOptions = new List<string>();
        textureOptions.Add("Full Resolution");
        textureOptions.Add("1/2 Resolution");
        textureOptions.Add("1/4 Resolution");
        textureOptions.Add("1/8 Resolution");

        _textureQualityDropdown.AddOptions(textureOptions);
        _textureQualityDropdown.RefreshShownValue();
    }

    public void LoadSettings()
    {
        //Gameplay Settings
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

        //Graphic Settings
        _resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", _currentResolutionIndex);
        PlayerPrefs.SetString("ResolutionString", _resolutionOptions[PlayerPrefs.GetInt("ResolutionIndex")]);
        PlayerPrefs.SetInt("ResolutionWidth", _resolutions[PlayerPrefs.GetInt("ResolutionIndex")].width);
        PlayerPrefs.SetInt("ResolutionHeight", _resolutions[PlayerPrefs.GetInt("ResolutionIndex")].width);
        _vsyncToggle.isOn = Convert.ToBoolean(PlayerPrefs.GetInt("Vsync", 0));
        _fullscreenDropdown.value = PlayerPrefs.GetInt("FullScreenOption", 0);
        _antiAliasingDropdown.value = PlayerPrefs.GetInt("AntiAliasing", 3);
        _shadowsDropdown.value = PlayerPrefs.GetInt("Shadows", 2);
        _softShadowQualityDropdown.value = PlayerPrefs.GetInt("SoftShadows", 2);
        _textureQualityDropdown.value = PlayerPrefs.GetInt("TextureQuality", 0);
    }

    public void ApplySettings()
    {
        //Gameplay
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

        //Graphics
        PlayerPrefs.SetInt("ResolutionIndex", _resolutionDropdown.value);
        PlayerPrefs.SetInt("ResolutionWidth", _resolutions[PlayerPrefs.GetInt("ResolutionIndex")].width);
        PlayerPrefs.SetInt("ResolutionHeight", _resolutions[PlayerPrefs.GetInt("ResolutionIndex")].height);
        PlayerPrefs.SetInt("Vsync", Convert.ToInt32(_vsyncToggle.isOn));
        PlayerPrefs.SetInt("FullScreenOption", _fullscreenDropdown.value);
        PlayerPrefs.SetInt("AntiAliasing", _antiAliasingDropdown.value);
        PlayerPrefs.SetInt("Shadows", _shadowsDropdown.value);
        PlayerPrefs.SetInt("SoftShadows", _softShadowQualityDropdown.value);
        PlayerPrefs.SetInt("TextureQuality", _textureQualityDropdown.value);

        GameManager.Instance.LoadGraphicSettings();

        StartCoroutine(SettingsApplying());

        if (_unappliedSettingsPanel.activeSelf)
        {
            _unappliedSettingsPanel.SetActive(false);
        }
    }

    public void SettingsChanged()
    {
        SettingsHasChanged = true;
    }

    private IEnumerator SettingsApplying()
    {
        SettingsApplied = true;
        yield return new WaitForEndOfFrame();
        SettingsApplied = false;
        SettingsHasChanged = false;
    }

    private float HorizontalToVertical(float fov)
    {
        float aspectRatio = (float)Screen.width / Screen.height;
        float verticalFOV = 2f * Mathf.Atan(Mathf.Tan(fov * Mathf.Deg2Rad / 2f) / aspectRatio) * Mathf.Rad2Deg;
        return verticalFOV;
    }
}
