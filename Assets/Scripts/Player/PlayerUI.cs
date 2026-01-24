using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI gunNameText;
    public TextMeshProUGUI ammoText;
    public Slider healthBar;
    public TextMeshProUGUI coinText;

    void Start()
    {
        if (gunNameText) gunNameText.text = "";
        if (ammoText) ammoText.text = "";
    }

    public void UpdateGunName(string gunName)
    {
        if (gunNameText)
            gunNameText.text = gunName;
    }

    public void UpdateAmmo(int current, int max)
    {
        if (ammoText == null)
            return;

        // Hide ammo whenever fists / no gun are equipped (max ammo = 0)
        if (max <= 0)
        {
            ammoText.text = "";
            return;
        }

        ammoText.text = $"{current} / {max}";
    }

    public void UpdateHealth(float current, float max)
    {
        if (healthBar)
            healthBar.value = current / max;
    }

    public void UpdateCoins(int coins)
    {
        if (coinText)
            coinText.text = coins.ToString();
    }
}
