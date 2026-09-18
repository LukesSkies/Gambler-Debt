
using System.Collections;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    private PlayerCurrentGun _playerCurrentGun;

    [SerializeField] private GameObject _grenadePrefab;

    [SerializeField] private float _timeUntilGunReset;
    [SerializeField] private float _throwForce;
    [SerializeField] private float _throwUpForce;

    private Transform _grenadePoint;

    public bool ThrowGrenade;
    public bool ThrowGrenadeAnimator;
    public bool DebugLine;
    public bool DebugLineToggle;

    private LineRenderer _lineRenderer;

    [Header("Grenade Line Renderer")]
    [Range(10, 100)][SerializeField]private int _linePoints;
    [Range(0.01f, 0.25f)][SerializeField]private float _timeBetweenPoints;

    private void Awake()
    {
        _playerCurrentGun = transform.root.GetComponent<PlayerCurrentGun>();
        _grenadePoint = transform.Find("GrenadePoint");
        _lineRenderer = _grenadePoint.GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if(ThrowGrenade && ThrowGrenadeAnimator)
        {
            ThrowGrenade = false;
            ThrowGrenadeAnimator = false;
            StartCoroutine(GrenadeThrow());
        }
        if (DebugLine && DebugLineToggle)
        {
            DrawProjection();
        }
        else
        {
            _lineRenderer.enabled = false;
        }
    }

    private void DrawProjection()
    {
        _lineRenderer.enabled = true;
        _lineRenderer.positionCount = Mathf.CeilToInt(_linePoints / _timeBetweenPoints) + 1;
        Vector3 startPosition = _grenadePoint.position;

        Vector3 forceDirection = Camera.main.transform.forward;

        RaycastHit hit;

        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 500f))
        {
            forceDirection = (hit.point - _grenadePoint.position).normalized;
        }

        Vector3 forceToAdd = forceDirection * _throwForce + transform.up * _throwUpForce;

        Vector3 startVelocity = forceToAdd;

        int i = 0;
        _lineRenderer.SetPosition(i, startPosition);
        for (float time = 0; time < _linePoints; time += _timeBetweenPoints)
        {
            i++;
            Vector3 point = startPosition + time * startVelocity;
            point.y = startPosition.y + startPosition.y * time + (Physics.gravity.y / 2 * time * time);

            _lineRenderer.SetPosition(i, point);
        }
    }

    private IEnumerator GrenadeThrow()
    {
        DebugLineToggle = false;

        GameObject grenade = Instantiate(_grenadePrefab, _grenadePoint.position, Camera.main.transform.rotation);
        Rigidbody grenadeRb = grenade.GetComponent<Rigidbody>();

        Vector3 forceDirection = Camera.main.transform.forward;

        RaycastHit hit;

        if(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 500f))
        {
            forceDirection = (hit.point - _grenadePoint.position).normalized;
        }

        Vector3 forceToAdd = forceDirection * _throwForce + transform.up * _throwUpForce;

        grenadeRb.AddForce(forceToAdd, ForceMode.Impulse);
        
        yield return new WaitForSeconds(_timeUntilGunReset);

        _playerCurrentGun.GrenadeGunReset();
    }
}