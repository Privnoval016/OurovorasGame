using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AudioSystem : MonoBehaviour
{
    #region Fields and Properties
    
    [Header("Pooling Settings")] 
    [SerializeField] private int defaultPoolSize = 10;

    private readonly Dictionary<AudioEvent, Queue<PooledEvent>> _pool = new();
    private readonly List<ActiveSnapshot> _activeSnapshots = new();

    private EventInstance _currentSnapshot;
    private EventInstance _currentMusic;
    private EventInstance _currentAmbient;
    
    private AudioEvent _currentMusicEvent;
    private AudioEvent _currentAmbientEvent;
    
    [Header("Audio Lookup Tables")]
    [SerializeField] private SurfaceLookupTable surfaceLookupTable;
    
    #endregion
    
    #region Wrapper Classes
    
    private class PooledEvent
    {
        public EventInstance Instance { get; }
        public AudioEvent Owner { get; }

        public PooledEvent(EventInstance instance, AudioEvent owner)
        {
            Instance = instance;
            Owner = owner;
        }
    }
    
    private class ActiveSnapshot
    {
        public AudioSnapshot snapshot;
        public EventInstance instance;

        public ActiveSnapshot(AudioSnapshot snapshot, EventInstance instance)
        {
            this.snapshot = snapshot;
            this.instance = instance;
        }
    }
    
    #endregion
    
    #region Audio Event Methods
    
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
     * <param name="owner">The AudioEvent that owns the instance.</param>
     * <param name="allowFadeout">Whether to allow fadeout when stopping the event (instance).</param>
     */
    public void StopEvent(EventInstance instance, AudioEvent owner, bool allowFadeout = true)
    {
        if (!instance.isValid()) return;

        instance.stop(allowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
        ReturnToPool(instance, owner);
    }
    
    #endregion

    #region Snapshot Methods
    /**
     * <summary>
     * Triggers an audio snapshot, stopping any currently active snapshot.
     * </summary>
     *
     * <param name="snapshot">The AudioSnapshot to trigger.</param>
     * <param name="priority">The priority of the snapshot (higher priority snapshots can override lower ones).</param>
     */
    public void TriggerSnapshot(AudioSnapshot snapshot, int priority = 0)
    {
        if (snapshot == null) return;

        // Stop lower-priority snapshots if needed
        for (int i = _activeSnapshots.Count - 1; i >= 0; i--)
        {
            var active = _activeSnapshots[i];
            if (active.snapshot.priority < priority)
            {
                StopSnapshot(active.snapshot); // fade out
            }
        }

        // Already active? Ignore
        if (_activeSnapshots.Exists(a => a.snapshot == snapshot)) return;

        // Create and start instance
        snapshot.snapshotDesc.createInstance(out EventInstance instance);
        if (instance.isValid())
        {
            instance.start();
            _activeSnapshots.Add(new ActiveSnapshot(snapshot, instance));
        }
    }

    /**
     * <summary>
     * Stops the specified audio snapshot.
     * </summary>
     *
     * <param name="snapshot">The AudioSnapshot to stop.</param>
     */
    public void StopSnapshot(AudioSnapshot snapshot)
    {
        if (snapshot == null) return;

        var active = _activeSnapshots.Find(a => a.snapshot == snapshot);
        if (active == null) return;

        active.instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        active.instance.release();
        _activeSnapshots.Remove(active);
    }
    
    /**
     * <summary>
     * Stops all currently active audio snapshots.
     * </summary>
     */
    public void StopAllSnapshots()
    {
        for (int i = _activeSnapshots.Count - 1; i >= 0; i--)
        {
            var active = _activeSnapshots[i];
            active.instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            active.instance.release();
        }

        _activeSnapshots.Clear();
    }
    
    #endregion
    
    #region Music Methods
    
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

        _currentMusicEvent = musicEvent;
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
     *
     * <param name="allowFadeout">Whether to allow fadeout when stopping the music.</param>
     */
    public void StopMusic(bool allowFadeout = true)
    {
        if (_currentMusic.isValid())
        {
            _currentMusic.stop(allowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            _currentMusic.release();
        }

        _currentMusic = default;
        _currentMusicEvent = null;
    }
    
    /**
     * <summary>
     * Transitions to new background music with optional crossfade.
     * </summary>
     *
     * <param name="newMusicEvent">The AudioEvent representing the new music to play.</param>
     * <param name="crossfadeTime">The duration of the crossfade transition in seconds.</param>
     */
    public void TransitionToMusic(AudioEvent newMusicEvent, float crossfadeTime = 1f)
    {
        if (_currentMusic.isValid())
        {
            var newInstance = RuntimeManager.CreateInstance(newMusicEvent.eventReference);
            newInstance.start();

            _currentMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _currentMusic.release();

            _currentMusic = newInstance;
            _currentMusicEvent = newMusicEvent;
        }
        else
        {
            StartMusic(newMusicEvent);
        }
    }
    
    #endregion
    
    #region Surface Based Audio Methods
    
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
        
        additionalParams ??= new AudioParamValue[] { };
        
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
    
    #endregion

    #region Ambient Sound Methods

    /**
     * <summary>
     * Starts playing ambient sound using the specified audio event.
     * </summary>
     *
     * <param name="ambientEvent">The AudioEvent representing the ambient sound to play.</param>
     * <param name="location">Optional transform to attach the ambient sound to for 3D audio.</param>
     */
    public void StartAmbient(AudioEvent ambientEvent, Transform location = null)
    {
        StopAmbient();

        _currentAmbientEvent = ambientEvent;
        _currentAmbient = RuntimeManager.CreateInstance(ambientEvent.eventReference);

        if (_currentAmbient.isValid())
        {
            if (location != null)
                RuntimeManager.AttachInstanceToGameObject(_currentAmbient, location, location.GetComponent<Rigidbody>());
            _currentAmbient.start();
        }
    }
    
    /**
     * <summary>
     * Stops the currently playing ambient sound, if any.
     * </summary>
     *
     * <param name="allowFadeout">Whether to allow fadeout when stopping the ambient sound.</param>
     */
    public void StopAmbient(bool allowFadeout = true)
    {
        if (_currentAmbient.isValid())
        {
            _currentAmbient.stop(allowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            _currentAmbient.release();
        }

        _currentAmbient = default;
        _currentAmbientEvent = null;
    }
    
    #endregion
    
    #region Pooling Methods
    
    private void InitializePool(AudioEvent evt)
    {
        if (_pool.ContainsKey(evt)) return;

        var queue = new Queue<PooledEvent>();
        int size = evt.poolSize > 0 ? evt.poolSize : defaultPoolSize;

        for (int i = 0; i < size; i++)
        {
            var instance = RuntimeManager.CreateInstance(evt.eventReference);
            queue.Enqueue(new PooledEvent(instance, evt));
        }

        _pool[evt] = queue;
    }

    private EventInstance GetPooledInstance(AudioEvent evt)
    {
        InitializePool(evt);

        var queue = _pool[evt];
        while (queue.Count > 0)
        {
            var pooled = queue.Dequeue();
            if (pooled.Instance.isValid())
                return pooled.Instance;
        }

        // Pool exhausted, create new instance
        return RuntimeManager.CreateInstance(evt.eventReference);
    }

    private void ReturnToPool(EventInstance instance, AudioEvent owner)
    {
        if (!instance.isValid()) return;

        if (_pool.TryGetValue(owner, out var queue))
        {
            if (queue.Count < owner.poolSize)
                queue.Enqueue(new PooledEvent(instance, owner));
            else
                instance.release(); // Pool full, release
        }
        else
        {
            instance.release(); // Fallback
        }

        RuntimeManager.AttachInstanceToGameObject(instance, transform, (Rigidbody) null);
    }
    
    #endregion
}