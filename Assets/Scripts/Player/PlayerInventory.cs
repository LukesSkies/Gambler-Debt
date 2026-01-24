using UnityEngine;
using TMPro;

public class PlayerInventory : MonoBehaviour
{
    public int coins = 0;

    [Header("UI")]
    public TextMeshProUGUI coinText;

    void Start()
    {
        UpdateUI();
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        UpdateUI();
    }

    public bool SpendCoins(int amount)
    {
        if (coins >= amount)
        {
            coins -= amount;
            UpdateUI();
            return true;
        }
        Debug.Log("Not enough coins!");
        return false;
    }

    void UpdateUI()
    {
        if (coinText != null)
            coinText.text = $"Coins: {coins}";
    }
}
