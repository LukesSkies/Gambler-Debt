using UnityEngine;

public class ZombieAttackCollider : MonoBehaviour
{
    private PlayerHP _playerHP;
    [SerializeField] private float _zombieDamage = 50f;
    [SerializeField] private Animation _redFlash;

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            _playerHP = other.GetComponentInParent<PlayerHP>();
            _playerHP.TakeDamage(_zombieDamage);
            _redFlash.Play();
        }
    }
}
