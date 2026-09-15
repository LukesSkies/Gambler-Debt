using UnityEngine;

public class ReviveQTE : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;

    private bool _checkingInput;

    private Transform _point;
    private Transform _pointStart;
    private Transform _pointEnd;

    private Vector3 targetPos;

    private void Start()
    {
        ResetMinigame();
    }

    private void Update()
    {
        if (!_checkingInput)
        {
            _point.position = Vector3.MoveTowards(_point.localPosition, targetPos, _moveSpeed * Time.deltaTime);

            if(Vector3.Distance(_point.localPosition, _pointStart.localPosition) < 0.1f)
            {
                targetPos = _pointEnd.localPosition;
            }
            else if (Vector3.Distance(_point.localPosition, _pointEnd.localPosition) < 0.1f)
            {
                targetPos = _pointStart.localPosition;
            }
        }
    }

    public void ResetMinigame()
    {
        int random = Random.Range(0, 2);

        if(random == 0)
        {
            targetPos = _pointStart.localPosition;
        }
        else
        {
            targetPos = _pointEnd.localPosition;
        }

        _point.position = new Vector3(Random.Range(_pointStart.position.x, _pointEnd.position.x), _point.position.y, _point.position.z);
    }
}
