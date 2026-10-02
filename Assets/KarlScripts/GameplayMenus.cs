using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameplayMenus : MonoBehaviour
{
    private GameObject _pauseMenu;
    private GameObject _settingsMenu;
    [SerializeField] private GameObject _resumeButton;
    [SerializeField] private GameObject _unappliedSettings;
    [SerializeField] private List<GameObject> _settingsOptions = new List<GameObject>();
    public PlayerCurrentGun PlayerCurrentGun;
    public PlayerMove PlayerMove;

    private SettingsMenu _settingsScript;

    private EventSystem _eventSystem;

    [HideInInspector] public GameObject GameOverMenu;
    [HideInInspector] public GameObject ReviveQTE;

    void Awake()
    {
        _pauseMenu = transform.Find("PauseMenu").gameObject;

        _resumeButton = _pauseMenu.transform.Find("Buttons").transform.Find("Resume").gameObject;

        _settingsMenu = transform.Find("SettingsMenu").gameObject;
        _settingsScript = _settingsMenu.GetComponent<SettingsMenu>();
        PlayerCurrentGun = GameObject.Find("NewPlayer0").GetComponent<PlayerCurrentGun>();
        PlayerMove = GameObject.Find("NewPlayer0").GetComponent<PlayerMove>();
        _eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();
        GameOverMenu = transform.Find("DeathMenu").gameObject;
        ReviveQTE = transform.Find("ReviveQTE").gameObject;
    }

    private void Start()
    {
        _pauseMenu.SetActive(false);
        GameOverMenu.SetActive(false);
    }

    public void Resume()
    {
        _pauseMenu.SetActive(false);
        GameManager.Instance.Paused = false;
        PlayerCurrentGun.CanShoot = true;
        StartCoroutine(ReEnableJump());
    }

    public void SettingsBack()
    {
        if (_settingsScript.SettingsHasChanged && !_settingsScript.SettingsApplied)
        {
            _unappliedSettings.SetActive(true);
        }
        else
        {
            _pauseMenu.SetActive(true);
            _settingsMenu.SetActive(false);
        }
    }

    public void SettingsMenu()
    {
        _pauseMenu.SetActive(false);
        _settingsMenu.SetActive(true);
        _settingsScript.SettingsHasChanged = false;

        for (int i = 0; i < _settingsOptions.Count; i++)
        {
            if(i == 0)
            {
                _settingsOptions[i].SetActive(true);
            }
            else
            {
                _settingsOptions[i].SetActive(false);
            }
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void Pause()
    {
        if (_pauseMenu.activeSelf)
        {
            Resume();
        }
        else if (!_pauseMenu.activeSelf)
        {
            _pauseMenu.SetActive(true);
            _settingsMenu.SetActive(false);
            if (!GameManager.Instance.Player0IsUsingKeyboardOrMouse)
            {
                _eventSystem.SetSelectedGameObject(_resumeButton);
            }
            else
            {
                _eventSystem.SetSelectedGameObject(null);
            }
            GameManager.Instance.Paused = true;
            PlayerCurrentGun.CanShoot = false;
            PlayerMove.CanJump = false;
        }
        else if (_settingsMenu.activeSelf)
        {
            if (_settingsScript.SettingsHasChanged && !_settingsScript.SettingsApplied)
            {
                _unappliedSettings.SetActive(true);
            }
            else
            {
                _pauseMenu.SetActive(true);
                _settingsMenu.SetActive(false);
            }
        }
    }

    public void SettingsToMenu()
    {
        _unappliedSettings.SetActive(false);
        _settingsScript.LoadSettings();
        _pauseMenu.SetActive(true);
        _settingsMenu.SetActive(false);
    }

    private IEnumerator ReEnableJump()
    {
        yield return new WaitForSeconds(0.1f);
        if (!GameManager.Instance.PlayerDead)
        {
            PlayerMove.CanJump = true;
        }
    }
}
