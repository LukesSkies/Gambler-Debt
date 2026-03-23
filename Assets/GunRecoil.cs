using UnityEngine;

public class GunRecoil : MonoBehaviour
{
    private Vector3 _currentRot;
    private Vector3 _targetRot;

    [HideInInspector] public GunSettings GunSettings;
    [HideInInspector] public bool IsAiming;

    void Update()
    {
        if(GunSettings != null)
        {
            _targetRot = Vector3.Lerp(_targetRot, Vector3.zero, GunSettings.ReturnSpeed * Time.deltaTime);
            _currentRot = Vector3.Slerp(_currentRot, _targetRot, GunSettings.Snappiness * Time.fixedDeltaTime);
        }

        transform.localRotation = Quaternion.Euler(_currentRot);
    }

    public void RecoilFire()
    {
        if (IsAiming && GunSettings != null)
        {
            _targetRot += new Vector3(GunSettings.AimRecoilX,
            Random.Range(-GunSettings.AimRecoilY, GunSettings.AimRecoilY),
            Random.Range(-GunSettings.AimRecoilZ, GunSettings.AimRecoilZ));
        }
        else if(!IsAiming && GunSettings != null)
        {
            _targetRot += new Vector3(GunSettings.HipfireRecoilX,
            Random.Range(-GunSettings.HipfireRecoilY, GunSettings.HipfireRecoilY),
            Random.Range(-GunSettings.HipfireRecoilZ, GunSettings.HipfireRecoilZ));
        }
    }
}
