using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameplayMenus : MonoBehaviour
{
    private GameObject _pauseMenu;
    private GameObject _settingsMenu;
    [SerializeField] private GameObject _resumeButton;
    public PlayerCurrentGun PlayerCurrentGun;
    public PlayerMove PlayerMove;

    private EventSystem _eventSystem;

    [HideInInspector] public GameObject DeathMenu;

    void Awake()
    {
        _pauseMenu = transform.Find("PauseMenu").gameObject;

        _resumeButton = _pauseMenu.transform.Find("Buttons").transform.Find("Resume").gameObject;

        _settingsMenu = transform.Find("SettingsMenu").gameObject;
        PlayerCurrentGun = GameObject.Find("NewPlayer0").GetComponent<PlayerCurrentGun>();
        PlayerMove = GameObject.Find("NewPlayer0").GetComponent<PlayerMove>();
        _eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();
        DeathMenu = transform.Find("DeathMenu").gameObject;
    }

    private void Start()
    {
        _pauseMenu.SetActive(false);
        DeathMenu.SetActive(false);
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
        _pauseMenu.SetActive(true);
        _settingsMenu.SetActive(false);
    }

    public void SettingsMenu()
    {
        _pauseMenu.SetActive(false);
        _settingsMenu.SetActive(true);
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
        if (_pauseMenu.activeSelf && !GameManager.Instance.EndGame)
        {
            Resume();
        }
        else if (!_pauseMenu.activeSelf && !GameManager.Instance.EndGame)
        {
            SettingsBack();
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
        else if (_settingsMenu.activeSelf && !GameManager.Instance.EndGame)
        {
            SettingsBack();
        }
    }

    private IEnumerator ReEnableJump()
    {
        yield return new WaitForSeconds(0.1f);
        PlayerMove.CanJump = true;
    }
}
