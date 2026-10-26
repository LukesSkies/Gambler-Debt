using UnityEngine;

public class KnifeAnimatorEvents : MonoBehaviour
{
    [SerializeField] private GameObject _knifeCollider;

    private PlayerCurrentGun _playerCurrentGun;

    private void Awake()
    {
        _playerCurrentGun = transform.root.GetComponent<PlayerCurrentGun>();
    }

    public void TakeDamage()
    {

    }

    public void ResetGun()
    {
        _playerCurrentGun.KnifeGunReset();
    }
}
