using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
    public static FMODEvents Instance { get; private set; }

    [field: Header("Player SFX")]
    [field: SerializeField] public EventReference PlayerFootsteps { get; private set; }

    [field: Header("Purchase")]
    [field: SerializeField] public EventReference Purchase { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Only one FMOD Events script should be in the scene");
        }

        Instance = this;
    }
}
