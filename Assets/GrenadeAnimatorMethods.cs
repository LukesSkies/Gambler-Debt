using UnityEngine;

public class GrenadeAnimatorMethods : MonoBehaviour
{
    private Grenade _grenade;

    private void Awake()
    {
        _grenade = transform.parent.GetComponent<Grenade>();
    }

    public void SetGrenadeAnimator()
    {
        _grenade.ThrowGrenadeAnimator = true;
        _grenade.DebugLineToggle = true;
    }
}