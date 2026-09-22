using System.Collections;
using UnityEngine;

public class GrenadeExplode : MonoBehaviour
{
    [SerializeField] private float _timeUntilExplode;
    [SerializeField] private GameObject _particles;
    void Start()
    {
        StartCoroutine(Explode());
    }

    private IEnumerator Explode()
    {
        yield return new WaitForSeconds(_timeUntilExplode);
        Instantiate(_particles, transform.position, _particles.transform.rotation);
        Destroy(gameObject);
    }
}
