using System.Collections;
using TMPro;
using UnityEngine;

public class PointAdditionMovement : MonoBehaviour
{
    [SerializeField] private Vector2 _upperLimit;
    [SerializeField] private Vector2 _lowerLimit;

    [SerializeField] private float _animationSpeed;
    [SerializeField] private float _textFadeLength;

    [SerializeField] private Vector3 _targetPos;

    private Coroutine _lerpTextFade;

    private TextMeshProUGUI _mainText;
    private TextMeshProUGUI _glowText;

    private float _time;

    private void Awake()
    {
        _mainText = GetComponent<TextMeshProUGUI>();
        _glowText = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        _targetPos = new Vector3(_upperLimit.x, Random.Range(_upperLimit.y, _lowerLimit.y), 0);

        _time = 0;
    }

    private void Update()
    {
        GetComponent<RectTransform>().localPosition = Vector3.MoveTowards(GetComponent<RectTransform>().localPosition, _targetPos, _animationSpeed * Time.deltaTime);

        if(_lerpTextFade == null)
        {
            _lerpTextFade = StartCoroutine(LerpTextFade());
        }
    }

    private IEnumerator LerpTextFade()
    {
        float time = 0;
        float startMainValue = _mainText.alpha;
        float startGlowValue = _glowText.alpha;

        while (time < _textFadeLength)
        {
            _mainText.alpha = Mathf.Lerp(startMainValue, 0, time / _textFadeLength);
            _glowText.alpha = Mathf.Lerp(startGlowValue, 0, time / _textFadeLength);
            time += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
