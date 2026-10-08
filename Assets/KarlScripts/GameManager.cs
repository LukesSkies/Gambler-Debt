using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if(_instance == null)
            {
                Debug.LogError("Need GameManager in the scene :(");
            }
            return _instance;
        }
    }

    [Header("Point Values")]
    public int HitPoints;
    public int EndTorsoPoints;
    public int EndHeadshotPoints;
    public int EndKnifePoints;

    [Header("Health Values")]
    public int ZombieHealth;
    public int PlayerHealth = 90;
    public int PlayerHealthRegen = 40;

    [Header("Player Values")]
    public int PlayerStamina = 4;

    [Header("Perk Values")]
    public int PlayerExtraHealth = 150;
    public int PlayerExtraStamina = 8;

    [Header("Game Values")]
    public bool Paused;
    public int Round;
    public int PlayerCount;
    public List<GameObject> GunGameObjects = new List<GameObject>();
    public List<GameObject> ZombieSpawnBarriers = new List<GameObject>();

    [Header("Player Values")]
    public bool Player0IsUsingKeyboardOrMouse;
    public bool PlayerDead;

    [Header("Game Referances")]
    public GameObject Zombie;

    [Header("UI")]
    public GameObject PointsAdditionText;
    public GameObject SlotItemPrefab;
    public List<Sprite> SlotMachineGunSprites = new List<Sprite>();

    [Header("SlotMachine")]
    public List<GameObject> SlotMachineGuns = new List<GameObject>();

    [Header("Graphics")]
    public int CurrentResolutionIndex;
    private Light _directionalLight;
    private UniversalAdditionalLightData _lightData;

    public int GetZombieHealth(int round)
    {
        if (round < 10)
        {
            return 50 + (100 * round);
        }
        else
        {
            return Mathf.RoundToInt(ZombieHealth * 1.1f);
        }
    }

    void Awake()
    {
        _instance = this;
        _directionalLight = GameObject.Find("Directional Light").GetComponent<Light>();
    }

    void Start()
    {
        LoadGraphicSettings();
        PlayerCount = 1;
        ZombieHealth = GetZombieHealth(1);
    }

    private void Update()
    {
        if (Paused && !PlayerDead)
        {
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        else if(!Paused && !PlayerDead)
        {
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void LoadGraphicSettings()
    {
        Screen.SetResolution(PlayerPrefs.GetInt("ResolutionWidth", 1920), PlayerPrefs.GetInt("ResolutionHeight", 1080), Screen.fullScreen);

        switch (PlayerPrefs.GetInt("FullScreenOption", 0))
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            case 1:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 2:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            default:
                break;
        }

        if(Convert.ToBoolean(PlayerPrefs.GetInt("Vsync", 0)))
        {
            QualitySettings.vSyncCount = 1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
        }

        switch (PlayerPrefs.GetInt("AntiAliasing", 3))
        {
            case 0:
                QualitySettings.antiAliasing = 0;
                break;
            case 1:
                QualitySettings.antiAliasing = 2;
                break;
            case 2:
                QualitySettings.antiAliasing = 4;
                break;
            case 3:
                QualitySettings.antiAliasing = 8;
                break;
            default:
                break;
        }

        switch (PlayerPrefs.GetInt("Shadows", 2))
        {
            case 0:
                QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
                _directionalLight.shadows = LightShadows.None;
                break;
            case 1:
                QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;
                _directionalLight.shadows = LightShadows.Hard;
                break;
            case 2:
                QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                _directionalLight.shadows = LightShadows.Soft;
                break;
            default:
                break;
        }

        if(_directionalLight.TryGetComponent(out _lightData))
        {
            switch (PlayerPrefs.GetInt("SoftShadows", 2))
            {
                case 0:
                    _lightData.softShadowQuality = SoftShadowQuality.Low;
                    break;
                case 1:
                    _lightData.softShadowQuality = SoftShadowQuality.Medium;
                    break;
                case 2:
                    _lightData.softShadowQuality = SoftShadowQuality.High;
                    break;
                default:
                    break;
            }
        }

        switch (PlayerPrefs.GetInt("TextureQuality", 0))
        {
            case 0:
                QualitySettings.globalTextureMipmapLimit = 0;
                break;
            case 1:
                QualitySettings.globalTextureMipmapLimit = 1;
                break;
            case 2:
                QualitySettings.globalTextureMipmapLimit = 2;
                break;
            case 3:
                QualitySettings.globalTextureMipmapLimit = 3;
                break;
            default:
                break;
        }
    }
}
