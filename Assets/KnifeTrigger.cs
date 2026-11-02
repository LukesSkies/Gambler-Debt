using UnityEngine;

public class KnifeTrigger : MonoBehaviour
{
    [SerializeField] private GameObject _knifeHitting;
    [SerializeField] private float _rayLength;

    RaycastHit _hit;

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Enemy")
        {
            _knifeHitting = other.GetComponent<GameObject>();
        }
        else
        {
            _knifeHitting = null;
        }
    }


    private void Update()
    {
        if(Physics.Raycast(transform.position, transform.forward, out _hit, _rayLength, LayerMask.GetMask("Enemy")))
        {
            _knifeHitting = _hit.transform.gameObject;
            Debug.DrawRay(transform.position, transform.forward * _rayLength, Color.green);
        }
        else
        {
            Debug.DrawRay(transform.position, transform.forward * _rayLength, Color.red);
        }
    }
}
