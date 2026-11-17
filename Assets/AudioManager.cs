using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance {  get; private set; }

    private void Awake()
    {
        if(Instance != null)
        {
            Debug.LogError("Only one Audio Manager should be in the scene");
        }

        Instance = this;
    }

    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    public EventInstance CreateEventInstance(EventReference eventReferance)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReferance);
        return eventInstance;
    }
}
