using UnityEngine;
using TMPro;

public class Points : MonoBehaviour
{
    public int Money = 500;

    private TextMeshProUGUI _pointText;
    private int _playerID;

    private Transform _pointParent;
    void Awake()
    {
        _pointParent = GameObject.Find("HUD").transform.Find("Points");

        _playerID = int.Parse(transform.name.Substring(9));

        _pointText = _pointParent.Find("Player" + _playerID.ToString()).transform.Find("PointText").GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        _pointText.text = Money.ToString();
    }
}
