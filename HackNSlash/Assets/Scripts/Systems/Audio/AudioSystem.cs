using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AudioSystem : MonoBehaviour
{
    [Header("Pooling Settings")]
    [SerializeField] private int poolSize = 20;

    private Dictionary<AudioEvent, Queue<EventInstance>> _pool = new();

    private EventInstance _currentSnapshot;
    private EventInstance _currentMusic;
    
    [Header("Audio Lookup Tables")]
    [SerializeField] private SurfaceLookupTable surfaceLookupTable;
    
    /**
     * <summary>
     * Plays an audio event with optional 3D attachment and parameters.
     * </summary>
     *
     * <param name="evt">The audio event to play.</param>
     * <param name="attachTo">Optional transform to attach the event to for 3D audio.</param>
     * <param name="start">Whether to start the event immediately.</param>
     * <param name="parameters">Optional array of audio parameters to set on the event.</param>
     *
     * <returns>The EventInstance of the played audio event.</returns>
     */
    public EventInstance PlayEvent(AudioEvent evt, Transform attachTo = null, bool start = true, AudioParamValue[] parameters = null)
    {
        EventInstance instance = GetPooledInstance(evt);

        if (!instance.isValid())
        {
            instance = RuntimeManager.CreateInstance(evt.eventReference);
            if (!instance.isValid())
            {
                Debug.LogWarning($"Failed to create EventInstance for {evt.name}");
                return default;
            }
        }

        // Default parameters
        foreach (var param in evt.defaultParameters)
            instance.setParameterByName(param.parameter.parameterName, param.value);

        // Runtime parameters
        if (parameters != null)
        {
            foreach (var p in parameters)
                instance.setParameterByName(p.parameter.parameterName, p.value);
        }
        
        instance.setPitch(Random.Range(evt.randomPitchRange.x, evt.randomPitchRange.y));
        instance.setVolume(Random.Range(evt.randomVolumeRange.x, evt.randomVolumeRange.y));

        // 3D attachment
        if (attachTo != null)
            RuntimeManager.AttachInstanceToGameObject(instance, attachTo, attachTo.GetComponent<Rigidbody>());
        else
            RuntimeManager.AttachInstanceToGameObject(instance, transform, (Rigidbody) null);

        if (start)
            instance.start();

        return instance;
    }
    
    /**
     * <summary>
     * Stops the specified audio event instance.
     * </summary>
     *
     * <param name="instance">The EventInstance to stop.</param>
     * <param name="allowFadeout">Whether to allow fadeout when stopping the event (instance).</param>
     */
    public void StopEvent(EventInstance instance, bool allowFadeout = true)
    {
        if (!instance.isValid()) return;

        instance.stop(allowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
        ReturnToPool(instance);
    }
    
    /**
     * <summary>
     * Triggers an audio snapshot, stopping any currently active snapshot.
     * </summary>
     *
     * <param name="snapshot">The AudioSnapshot to trigger.</param>
     */
    public void TriggerSnapshot(AudioSnapshot snapshot)
    {
        StopSnapshot();

        if (snapshot != null)
        {
            snapshot.snapshotDesc.createInstance(out _currentSnapshot);
            if (_currentSnapshot.isValid())
                _currentSnapshot.start();
        }
    }

    /**
     * <summary>
     * Stops the currently active audio snapshot, if any.
     * </summary>
     */
    public void StopSnapshot()
    {
        if (_currentSnapshot.isValid())
            _currentSnapshot.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        _currentSnapshot = default;
    }
    
    /**
     * <summary>
     * Starts playing background music using the specified audio event.
     * </summary>
     *
     * <param name="musicEvent">The AudioEvent representing the music to play.</param>
     */
    public void StartMusic(AudioEvent musicEvent)
    {
        StopMusic();

        _currentMusic = RuntimeManager.CreateInstance(musicEvent.eventReference);
        if (_currentMusic.isValid())
            _currentMusic.start();
    }

    /**
     * <summary>
     * Sets a parameter for the currently playing music.
     * </summary>
     *
     * <param name="param">The AudioParameter to set.</param>
     * <param name="value">The value to set for the parameter.</param>
     */
    public void SetMusicParameter(AudioParameter param, float value)
    {
        if (_currentMusic.isValid())
            _currentMusic.setParameterByName(param.parameterName, value);
    }

    /**
     * <summary>
     * Stops the currently playing music, if any.
     * </summary>
     */
    public void StopMusic()
    {
        if (_currentMusic.isValid())
        {
            _currentMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _currentMusic.release();
        }

        _currentMusic = default;
    }
    
    
    /**
     * <summary>
     * Plays a footstep sound based on the surface type at the target's position.
     * </summary>
     *
     * <param name="footstepEvent">The AudioEvent representing the footstep sound.</param>
     * <param name="target">The Transform of the character making the footstep.</param>
     * <param name="additionalParams">Optional array of additional audio parameters to set.</param>
     */
    public void PlaySurfaceSound(AudioEvent footstepEvent, Transform target, AudioParamValue[] additionalParams = null)
    {
        var surfaceParam = GetSurfaceParam(target);
        
        if (surfaceParam.parameter != null)
            AudioParamValue.ReplaceParameter(additionalParams, surfaceParam);

        PlayEvent(footstepEvent, target, true, additionalParams);
    }

    private AudioParamValue GetSurfaceParam(Transform target)
    {
        if (Physics.Raycast(target.position + Vector3.up * 0.1f, 
                Vector3.down, out RaycastHit hit, 1f))
        {
            // might be too expensive to GetComponent every time?
            if (surfaceLookupTable.TryGetValue(hit.collider.GetComponent<MeshRenderer>().material, out var surfaceType))
            {
                return surfaceType.param;
            }

            return surfaceLookupTable.GetDefaultValue()?.param ?? default;
        }

        return default;
    }

    /** <summary>
     * Plays an ambient sound at a specified location with optional parameters.
     * </summary>
     *
     * <param name="ambientEvent">The AudioEvent representing the ambient sound.</param>
     * <param name="location">The Transform location to attach the sound to.</param>
     * <param name="parameters">Optional array of audio parameters to set on the ambient sound.</param>
     */
    public void PlayAmbient(AudioEvent ambientEvent, Transform location, AudioParamValue[] parameters = null)
    {
        PlayEvent(ambientEvent, location, true, parameters);
    }
    
    private EventInstance GetPooledInstance(AudioEvent evt)
    {
        if (!_pool.TryGetValue(evt, out var queue))
            _pool[evt] = queue = new Queue<EventInstance>();

        while (queue.Count > 0)
        {
            var instance = queue.Dequeue();
            if (instance.isValid())
                return instance;
        }

        var newInstance = RuntimeManager.CreateInstance(evt.eventReference);
        if (!newInstance.isValid())
            Debug.LogWarning($"Failed to create new EventInstance for {evt.name}");
        return newInstance;
    }

    private void ReturnToPool(EventInstance instance)
    {
        if (!instance.isValid()) return;

        RuntimeManager.AttachInstanceToGameObject(instance, transform, (Rigidbody) null);

        foreach (var kvp in _pool)
        {
            if (kvp.Value.Count < poolSize)
            {
                kvp.Value.Enqueue(instance);
                return;
            }
        }

        instance.release();
    }
}