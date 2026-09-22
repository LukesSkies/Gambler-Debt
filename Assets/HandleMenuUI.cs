using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class HandleMenuUI : MonoBehaviour
{
    public static HandleMenuUI instance;

    public List<GameObject> Buttons = new List<GameObject>();

    public GameObject LastSelectedButton;
    public int LastSelectedIndex;

    [SerializeField] private PlayerInputHandler _playerInputHandler;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        _playerInputHandler = GameObject.Find("NewPlayer0").GetComponent<PlayerInputHandler>();
    }

    void Update()
    {
        //Moving Up
        if(_playerInputHandler.NavigationInput.y < 0)
        {
            HandleNextButtonSelection(1);
        }

        //Moving Down
        if (_playerInputHandler.NavigationInput.y > 0)
        {
            HandleNextButtonSelection(-1);
        }
    }

    private void HandleNextButtonSelection(int addition)
    {
        if (EventSystem.current.currentSelectedGameObject == null && LastSelectedButton != null)
        {
            int newIndex = LastSelectedIndex + addition;
            newIndex = Mathf.Clamp(newIndex, 0, Buttons.Count - 1);
            EventSystem.current.SetSelectedGameObject(Buttons[newIndex]);
        }
    }
}
