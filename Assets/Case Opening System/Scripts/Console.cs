using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Console : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _consolePanel;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Transform _contentPanel;
    [SerializeField] private GameObject _textPrefab;

    private Inventory _inventory;

    private Dictionary<string, System.Action<string[]>> _commands;

    private bool _isOpen;

    private void Awake()
    {
        _inventory = GetComponent<Inventory>();

        if (_inventory == null)
        {
            this.enabled = false;
            return;
        }
    }

    private void Start()
    {
        _commands = new Dictionary<string, System.Action<string[]>>();
        _commands.Add("help", CmdHelp);
        _commands.Add("clear", CmdClear);
        _commands.Add("items", CmdItems);
        _commands.Add("give", CmdGive);
        _commands.Add("save", CmdSave);
        _commands.Add("load", CmdLoad);
        _commands.Add("open", CmdOpen);

        HandleInput("give 0");
        HandleInput("give 0");
        HandleInput("give 0");
        HandleInput("give 0");
        HandleInput("give 0");
        HandleInput("give 0");
        HandleInput("give 0");
        HandleInput("give 0");
        HandleInput("give 0");
        HandleInput("give 0");

        HandleInput("give 1");
        HandleInput("give 1");
        HandleInput("give 1");
        HandleInput("give 1");
        HandleInput("give 1");
        HandleInput("give 1");
        HandleInput("give 1");
        HandleInput("give 1");
        HandleInput("give 1");
        HandleInput("give 1");

        HandleInput("clear");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            _isOpen = !_isOpen;
            _consolePanel.gameObject.SetActive(_isOpen);
        }

        if (Input.GetKeyDown(KeyCode.Return) && _isOpen)
        {
            SubmitMessage();
        }
    }

    private void HandleInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            Print($">");
            return;
        }

        string[] parts = input.Split(' ');
        string command = parts[0].ToLower();
        string[] args = new string[parts.Length - 1];
        for (int i = 1; i < parts.Length; i++)
        {
            args[i - 1] = parts[i];
        }

        if (_commands.ContainsKey(command))
        {
            _commands[command].Invoke(args);
        }
        else
        {
            Print($"Unknown command: {command}");
        }

        _inputField.text = "";
        _inputField.ActivateInputField();
    }

    private void Print(string message)
    {
        GameObject textObj = Instantiate(_textPrefab, _contentPanel);
        TMP_Text textComp = textObj.GetComponent<TMP_Text>();

        if (textComp != null)
        {
            textComp.text = message;
        }
    }

    public void SubmitMessage()
    {
        HandleInput(_inputField.text);
    }

    public void CloseConsole()
    {
        _isOpen = false;
        _consolePanel.gameObject.SetActive(false);
    }

    // Commands
    private void CmdHelp(string[] args)
    {
        Print("Available commands:");
        Print("help - show this help");
        Print("clear - clear console output");
        Print("items - show list with all items");
        Print("give <id> - give item by ID");
        Print("save <file_name> - save data to file");
        Print("load <file_name> - load data from file");
        Print("open <case_id> - open case by ID");
    }

    private void CmdClear(string[] args)
    {
        foreach (Transform child in _contentPanel)
        {
            Destroy(child.gameObject);
        }
    }

    private void CmdGive(string[] args)
    {
        if (args.Length == 0)
        {
            Print("Usage: give <id>");
            return;
        }

        int id;
        if (int.TryParse(args[0], out id))
        {
            Item item = ItemRegistry.GetItemById(id);

            if (item != null)
            {
                if (_inventory != null)
                {
                    _inventory.AddItem(item);
                    Print($"Item by ID:{id} added");
                }
                else
                {
                    Print($"Inventory not founded");
                }
            }
            else
            {
                Print($"Item by ID:{id} doesn't exist");
            }
        }
        else
        {
            Print("Invalid ID!");
        }
    }

    private void CmdSave(string[] args)
    {
        if (args.Length == 0)
        {
            Print("Usage: save <file_name>");
            return;
        }

        string fileName = args[0];

        if (_inventory != null)
        {
            _inventory.Save(fileName);
        }

        Print($"Saving data to {Application.persistentDataPath}");
    }

    private void CmdLoad(string[] args)
    {
        if (args.Length == 0)
        {
            Print("Usage: load <file_name>");
            return;
        }

        string fileName = args[0];

        if (_inventory != null)
        {
            _inventory.Load(fileName);
        }

        Print($"Loading data from {Application.persistentDataPath}");
    }

    private void CmdItems(string[] args)
    {
        Item[] items = ItemRegistry.GetItemsSortedById();

        foreach (var item in items)
        {
            Print($"{item.Id} | \"{item.ItemType.ToString()}\" | {item.Name}");
        }
    }

    private void CmdOpen(string[] args)
    {
        if (args.Length == 0)
        {
            Print("Usage: open <id>");
            return;
        }

        int id;
        if (int.TryParse(args[0], out id))
        {
            Item item = ItemRegistry.GetItemById(id);

            if (item != null)
            {
                if (item.ItemType == ItemType.Case)
                {
                    Item drop = CaseDropSystem.GetRandomDrop(item as CaseItem);

                    if (drop != null)
                    {
                        Print($"Drop: {drop.Name}");
                        
                        if (_inventory != null)
                        {
                            _inventory.AddItem(drop);
                        }
                    }
                    else
                    {
                        Print($"Drop is null");
                    }
                }
                else
                {
                    Print("This item is not a case");
                }
            }
            else
            {
                Print("Invalid ID!");
            }
        }
    }
}