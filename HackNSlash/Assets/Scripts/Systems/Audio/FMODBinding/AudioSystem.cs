using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using Random = UnityEngine.Random;

public class AudioSystem : MonoBehaviour, IAudioSystem
{
    #region Fields and Properties
    
    [Header("Pooling Settings")] 
    [SerializeField] private int defaultPoolSize = 10;

    private readonly Dictionary<AudioEvent, Queue<PooledEvent>> _pool = new();
    private readonly List<ActiveSnapshot> _activeSnapshots = new();
    
    private EventInstance _currentMusic;
    private EventInstance _currentAmbient;
    
    private AudioEvent _currentMusicEvent;
    private AudioEvent _currentAmbientEvent;
    
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
    
    #region MonoBehaviour Callbacks

    private void OnDestroy()
    {
        StopAllSnapshots();
        StopMusic(false);
        StopAmbient(false);
        
        foreach (var kvp in _pool) 
        {
            var queue = kvp.Value;
            while (queue.Count > 0)
            {
                var pooled = queue.Dequeue();
                if (pooled.Instance.isValid())
                    pooled.Instance.release();
            }
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
            //Attach callback
            instance.setCallback(OnEventEnd);
            // Store owner reference for callback
            instance.setUserData(GCHandle.ToIntPtr(GCHandle.Alloc(evt)));

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

        // 3D attachment
        if (attachTo != null)
            RuntimeManager.AttachInstanceToGameObject(instance, attachTo.gameObject, attachTo.GetComponent<Rigidbody>());

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
    
    private FMOD.RESULT OnEventEnd(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parametersPtr)
    {
        if (type != EVENT_CALLBACK_TYPE.STOPPED)
            return FMOD.RESULT.OK;

        // Convert IntPtr back to EventInstance
        var instance = new EventInstance(instancePtr);

        // Get stored owner reference
        instance.getUserData(out IntPtr data);
        if (data == IntPtr.Zero)
            return FMOD.RESULT.OK;

        var handle = GCHandle.FromIntPtr(data);
        var owner = (AudioEvent)handle.Target;

        ReturnToPool(instance, owner);

        handle.Free();

        return FMOD.RESULT.OK;
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
        if (!instance.isValid())
        {
            instance.release();
            return;
        }

        instance.start();
        _activeSnapshots.Add(new ActiveSnapshot(snapshot, instance));

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
        if (!active.instance.isValid()) return;

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
    
    public void SetSnapshotParameter(AudioSnapshot snapshot, AudioParameter param, float value)
    {
        var active = _activeSnapshots.Find(a => a.snapshot == snapshot);
        if (active != null && active.instance.isValid())
        {
            active.instance.setParameterByName(param.parameterName, value);
        }
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
     * Transitions to new background music.
     * </summary>
     *
     * <param name="newMusicEvent">The AudioEvent representing the new music to play.</param>
     */
    public void TransitionToMusic(AudioEvent newMusicEvent)
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
        var paramFound = GetSurfaceParam(target, out AudioParamValue surfaceParam);
        
        additionalParams ??= new AudioParamValue[] { };
        
        if (paramFound)
            AudioParamValue.ReplaceParameter(additionalParams, surfaceParam);

        PlayEvent(footstepEvent, target, true, additionalParams);
    }

    private bool GetSurfaceParam(Transform target, out AudioParamValue param)
    {
        if (Physics.Raycast(target.position + Vector3.up * 0.1f, 
                Vector3.down, out RaycastHit hit, 1f))
        {
            // might be too expensive to TryGetComponent every time?
            if (hit.collider.TryGetComponent(out InstanceParameter surface))
            {
                param = surface.instanceParam.param;
                return true;
            }
        }

        param = new AudioParamValue();
        return false;
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
                RuntimeManager.AttachInstanceToGameObject(_currentAmbient, location.gameObject);
            _currentAmbient.start();
        }
    }
    
    /**
     * <summary>
     * Sets a parameter for the currently playing ambient sound.
     * </summary>
     *
     * <param name="param">The AudioParameter to set.</param>
     * <param name="value">The value to set for the parameter.</param>
     */
    public void SetAmbientParameter(AudioParameter param, float value)
    {
        if (_currentAmbient.isValid())
            _currentAmbient.setParameterByName(param.parameterName, value);
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
            if (!pooled.Instance.isValid()) continue;

            ResetInstance(pooled.Instance);
            return pooled.Instance;
        }


        // Pool exhausted, create new instance
        return RuntimeManager.CreateInstance(evt.eventReference);
    }
    
    private void ResetInstance(EventInstance instance)
    {
        if (!instance.isValid()) return;

        instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        instance.setTimelinePosition(0);
    }


    private void ReturnToPool(EventInstance instance, AudioEvent owner)
    {
        if (!instance.isValid()) return;

        if (_pool.TryGetValue(owner, out var queue))
        {
            int poolSize = owner.poolSize > 0 ? owner.poolSize : defaultPoolSize;
            if (queue.Count < poolSize)
                queue.Enqueue(new PooledEvent(instance, owner));
            else
                instance.release(); // Pool full, release
        }
        else
        {
            instance.release(); // Fallback
        }
    }
    
    #endregion
}

public interface IAudioSystem : IService
{
    EventInstance PlayEvent(AudioEvent evt, Transform attachTo = null, bool start = true, AudioParamValue[] parameters = null);
    void StopEvent(EventInstance instance, AudioEvent owner, bool allowFadeout = true);
    void TriggerSnapshot(AudioSnapshot snapshot, int priority = 0);
    void StopSnapshot(AudioSnapshot snapshot);
    void StopAllSnapshots();
    void SetSnapshotParameter(AudioSnapshot snapshot, AudioParameter param, float value);
    void StartMusic(AudioEvent musicEvent);
    void SetMusicParameter(AudioParameter param, float value);
    void StopMusic(bool allowFadeout = true);
    void TransitionToMusic(AudioEvent newMusicEvent);
    void PlaySurfaceSound(AudioEvent footstepEvent, Transform target, AudioParamValue[] additionalParams = null);
    void StartAmbient(AudioEvent ambientEvent, Transform location = null);
    void SetAmbientParameter(AudioParameter param, float value);
    void StopAmbient(bool allowFadeout = true);
}