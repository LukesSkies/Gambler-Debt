using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComboUI : MonoBehaviour
{
    public Slider comboSlider;
    public TextMeshProUGUI comboText;
    //public TextMeshProUGUI multikillText;

    private float multikillDisplayTime = 1.2f;
    private float multikillTimer = 0f;

    void Start()
    {
        comboSlider.maxValue = ComboSystem.Instance.maxCombo;

        ComboSystem.Instance.OnComboChanged += UpdateComboUI;
        ComboSystem.Instance.OnMultikill += ShowMultikill;
    }

    void UpdateComboUI()
    {
        float combo = ComboSystem.Instance.combo;
        comboSlider.value = combo;

        float dmgMult = ComboSystem.Instance.GetDamageMultiplier();
        comboText.text = $"Combo: {Mathf.RoundToInt(combo)} (x{dmgMult:F1})";
    }

    void ShowMultikill(int count)
    {
        multikillTimer = multikillDisplayTime;
        //multikillText.text = $"{count}x MULTIKILL!";
        //multikillText.alpha = 1f;
    }

    void Update()
    {
        if (multikillTimer > 0)
        {
            multikillTimer -= Time.deltaTime;
            //multikillText.alpha = multikillTimer / multikillDisplayTime;
        }
    }
}
