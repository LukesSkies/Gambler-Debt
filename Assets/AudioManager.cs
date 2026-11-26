using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AudioManager : MonoBehaviour
{
    private List<EventInstance> _eventInstances;

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Only one Audio Manager should be in the scene");
        }

        Instance = this;

        _eventInstances = new List<EventInstance>();
    }

    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    public EventInstance CreateEventInstance(EventReference eventReferance)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReferance);
        _eventInstances.Add(eventInstance);
        return eventInstance;
    }

    public EventInstance CreateOneShotInstance(EventReference eventReferance)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReferance);
        _eventInstances.Add(eventInstance);
        return eventInstance;
    }

    public void SetAudioPaused(string busName, bool paused)
    {
        RuntimeManager.GetBus("bus:/" + busName).setPaused(paused);
    }

    private void CleanUp()
    {
        foreach (EventInstance eventInstance in _eventInstances)
        {
            eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            eventInstance.release();
        }
    }

    private void OnDestroy()
    {
        CleanUp();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}