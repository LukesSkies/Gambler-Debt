using UnityEngine;
using TMPro;

public class Points : MonoBehaviour
{
    public int Money = 500;

    private TextMeshProUGUI _pointText;
    private int _playerID;

    private Transform _pointTextParent;

    private Transform _pointParent;
    void Awake()
    {
        _pointParent = GameObject.Find("HUD").transform.Find("Points");

        _playerID = int.Parse(transform.name.Substring(9));

        _pointText = _pointParent.Find("Player" + _playerID.ToString()).transform.Find("PointText").GetComponent<TextMeshProUGUI>();

        _pointTextParent = GameObject.Find("HUD").transform.Find("Points").transform.Find("Player0").transform.Find("PointAdditionParent");
    }

    void Update()
    {
        if(_pointText.text != Money.ToString())
        {
            _pointText.text = Money.ToString();
        }
    }

    public void AddPoints(int pointAmmount)
    {
        Money += pointAmmount;
        GameObject pointText = Instantiate(GameManager.Instance.PointsAdditionText, _pointTextParent);

        TextMeshProUGUI pointTextUI = pointText.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI pointGlowTextUI = pointText.transform.GetChild(0).GetComponent<TextMeshProUGUI>();

        pointTextUI.color = Color.yellow;
        pointGlowTextUI.color = Color.yellow;

        pointTextUI.text = "+" + pointAmmount.ToString();
        pointGlowTextUI.text = "+" + pointAmmount.ToString();
    }

    public void RemovePoints(int pointAmmount)
    {
        Money -= pointAmmount;
        GameObject pointText = Instantiate(GameManager.Instance.PointsAdditionText, _pointTextParent);

        TextMeshProUGUI pointTextUI = pointText.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI pointGlowTextUI = pointText.transform.GetChild(0).GetComponent<TextMeshProUGUI>();

        pointTextUI.color = Color.red;
        pointGlowTextUI.color = Color.red;

        pointTextUI.text = "-" + pointAmmount.ToString();
        pointGlowTextUI.text = "-" + pointAmmount.ToString();
    }
}
