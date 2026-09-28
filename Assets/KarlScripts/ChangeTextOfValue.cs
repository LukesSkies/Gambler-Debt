using TMPro;
using UnityEngine;

public class ChangeTextOfValue : MonoBehaviour
{
    private TextMeshProUGUI _sliderText;

    private void Awake()
    {
        _sliderText = transform.Find("Value").GetComponent<TextMeshProUGUI>();
    }

    public void OnSliderChange(float value)
    {
        _sliderText.text = value.ToString();
    }
}
