using UnityEngine;

public class DestoryParticles : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    void Start()
    {
        var main = _particleSystem.main;
        main.stopAction = ParticleSystemStopAction.Destroy;
    }
}
