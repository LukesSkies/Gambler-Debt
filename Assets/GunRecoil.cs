using UnityEngine;

public class GunRecoil : MonoBehaviour
{
    private Vector3 _currentRot;
    private Vector3 _targetRot;

    [Header("Hipfire")]
    [SerializeField] private float _recoilX;
    [SerializeField] private float _recoilY;
    [SerializeField] private float _recoilZ;

    [Header("Settings")]
    [SerializeField] private float _snappiness;
    [SerializeField] private float _returnSpeed;

    void Update()
    {
        _targetRot = Vector3.Lerp(_targetRot, Vector3.zero, _returnSpeed * Time.deltaTime);
        _currentRot = Vector3.Slerp(_currentRot, _targetRot, _snappiness * Time.fixedDeltaTime);

        transform.localRotation = Quaternion.Euler(_currentRot);
    }

    public void RecoilFire()
    {
        _targetRot += new Vector3(_recoilX, Random.Range(-_recoilY, _recoilY), Random.Range(-_recoilZ, _recoilZ));
    }
}
