using UnityEngine;

public class KnifeTrigger : MonoBehaviour
{
    [SerializeField] private GameObject _knifeHitting;

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
}
