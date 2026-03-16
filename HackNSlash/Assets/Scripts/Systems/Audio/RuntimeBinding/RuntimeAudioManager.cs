using System;
using Extensions.EventBus;
using UnityEngine;

public class RuntimeAudioManager : MonoBehaviour
{
    [Header("Audio References")]
    [SerializeField] private AudioSystem AudioSystem;
    [SerializeField] private AudioLookupAtlas AudioLookupAtlas;
    
    private EventBinding<PlaySFXEvent> _playSFXBinding;
    private EventBinding<PlayMusicEvent> _playMusicBinding;
    private EventBinding<StopMusicEvent> _stopMusicBinding;
    private EventBinding<ApplyAmbienceEvent> _applyAmbienceBinding;
    private EventBinding<RemoveAmbienceEvent> _removeAmbienceBinding;
    private EventBinding<ApplySnapshotEvent> _applySnapshotBinding;
    private EventBinding<RemoveSnapshotEvent> _removeSnapshotBinding;

    private void Start()
    {
        //EventBus<PlayMusicEvent>.Raise(new PlayMusicEvent(AudioLookupAtlas.Instance.explorationMusicEvent));
    }

    private void OnEnable()
    {
        AudioLookupAtlas.Initialize(AudioLookupAtlas);
        
        _playSFXBinding = new EventBinding<PlaySFXEvent>(OnPlaySFX);
        EventBus<PlaySFXEvent>.Register(_playSFXBinding);
        
        _playMusicBinding = new EventBinding<PlayMusicEvent>(OnPlayMusic);
        EventBus<PlayMusicEvent>.Register(_playMusicBinding);
        
        _stopMusicBinding = new EventBinding<StopMusicEvent>(OnStopMusic);
        EventBus<StopMusicEvent>.Register(_stopMusicBinding);
        
        _applyAmbienceBinding = new EventBinding<ApplyAmbienceEvent>(OnApplyAmbience);
        EventBus<ApplyAmbienceEvent>.Register(_applyAmbienceBinding);
        
        _removeAmbienceBinding = new EventBinding<RemoveAmbienceEvent>(OnRemoveAmbience);
        EventBus<RemoveAmbienceEvent>.Register(_removeAmbienceBinding);
        
        _applySnapshotBinding = new EventBinding<ApplySnapshotEvent>(OnApplySnapshot);
        EventBus<ApplySnapshotEvent>.Register(_applySnapshotBinding);
        
        _removeSnapshotBinding = new EventBinding<RemoveSnapshotEvent>(OnRemoveSnapshot);
        EventBus<RemoveSnapshotEvent>.Register(_removeSnapshotBinding);
    }
    
    private void OnDisable()
    {
        EventBus<PlaySFXEvent>.Deregister(_playSFXBinding);
        EventBus<PlayMusicEvent>.Deregister(_playMusicBinding);
        EventBus<StopMusicEvent>.Deregister(_stopMusicBinding);
        EventBus<ApplyAmbienceEvent>.Deregister(_applyAmbienceBinding);
        EventBus<RemoveAmbienceEvent>.Deregister(_removeAmbienceBinding);
        EventBus<ApplySnapshotEvent>.Deregister(_applySnapshotBinding);
        EventBus<RemoveSnapshotEvent>.Deregister(_removeSnapshotBinding);
    }
    
    #region Callbacks
   
    private void OnPlaySFX(PlaySFXEvent evt)
    {
        if (evt.CheckSurface)
        {
            AudioSystem.PlaySurfaceSound(evt.AudioEvent, evt.AttachTo, evt.Parameters);
            return;
        }
        
        AudioSystem.PlayEvent(evt.AudioEvent, evt.AttachTo, true, evt.Parameters);
    }

    private void OnPlayMusic(PlayMusicEvent evt)
    {
        if (evt.MusicEvent != null) // if null, the user just wants to set parameters
            AudioSystem.StartMusic(evt.MusicEvent);

        if (evt.Parameters == null) return;
        foreach (var param in evt.Parameters)
        {
            AudioSystem.SetMusicParameter(param.parameter, param.value);
        }
    }
    
    private void OnStopMusic(StopMusicEvent evt)
    {
        AudioSystem.StopMusic(evt.FadeOut);
    }
    
    private void OnApplyAmbience(ApplyAmbienceEvent evt)
    {
        if (evt.AmbienceEvent != null) // if null, the user just wants to set parameters
            AudioSystem.StartAmbient(evt.AmbienceEvent);

        if (evt.Parameters == null) return;
        foreach (var param in evt.Parameters)
        {
            AudioSystem.SetAmbientParameter(param.parameter, param.value);
        }
    }
    
    private void OnRemoveAmbience(RemoveAmbienceEvent evt)
    {
        AudioSystem.StopAmbient(evt.FadeOut);
    }
    
    private void OnApplySnapshot(ApplySnapshotEvent evt)
    {
        AudioSystem.TriggerSnapshot(evt.Snapshot);

        if (evt.Snapshot == null) return;
        foreach (var param in evt.Parameters)
        {
            AudioSystem.SetSnapshotParameter(evt.Snapshot, param.parameter, param.value);
        }
    }
    
    private void OnRemoveSnapshot(RemoveSnapshotEvent evt)
    {
        if (evt.RemoveAll)
        {
            AudioSystem.StopAllSnapshots();
            return;
        }

        if (evt.Snapshots == null) return;
        foreach (var snapshot in evt.Snapshots)
        {
            AudioSystem.StopSnapshot(snapshot);
        }
    }
    
    #endregion
}

#region SFX Events

/**
 * <summary>
 * Event to play a sound effect (SFX) at a specified location or attached to a transform.
 * </summary>
 *
 */
public struct PlaySFXEvent : IEvent
{
    public readonly AudioEvent AudioEvent;
    public readonly Transform AttachTo;
    public readonly AudioParamValue[] Parameters;
    public readonly bool CheckSurface;
    
    public PlaySFXEvent(AudioEvent audioEvent, Transform attachTo = null, 
        AudioParamValue[] parameters = null, bool checkSurface = false)
    {
        AudioEvent = audioEvent;
        AttachTo = attachTo;
        Parameters = parameters;
        CheckSurface = checkSurface;
    }
}

#endregion

#region Music Events

/**
 * <summary>
 * Event to play background music with optional parameters.
 * </summary>
 */
public struct PlayMusicEvent : IEvent
{
    public readonly AudioEvent MusicEvent;
    public readonly AudioParamValue[] Parameters;
    
    /** <summary>
     * Constructor to play a music event with optional parameter changes.
     * </summary>
     */
    public PlayMusicEvent(AudioEvent musicEvent, AudioParamValue[] parameters = null)
    {
        MusicEvent = musicEvent;
        Parameters = parameters;
    }
    
    /** <summary>
     * Constructor to apply only parameter changes without a music event.
     * </summary>
     */
    public PlayMusicEvent(AudioParamValue[] parameters)
    {
        MusicEvent = null;
        Parameters = parameters;
    }
}

/**
 * <summary>
 * Event to stop the currently playing background music.
 * </summary>
 */
public struct StopMusicEvent : IEvent
{
    public readonly bool FadeOut;
    
    public StopMusicEvent(bool fadeOut = true)
    {
        FadeOut = fadeOut;
    }
}

#endregion

#region Ambience Events

/**
 * <summary>
 * Event to apply ambient sounds with optional parameters.
 * </summary>
 */
public struct ApplyAmbienceEvent : IEvent
{
    public readonly AudioEvent AmbienceEvent;
    public readonly AudioParamValue[] Parameters;
    
    /** <summary>
     * Constructor to apply an ambience event with optional parameter changes.
     * </summary>
     */
    public ApplyAmbienceEvent(AudioEvent ambienceEvent, AudioParamValue[] parameters = null)
    {
        AmbienceEvent = ambienceEvent;
        Parameters = parameters;
    }
    
    /** <summary>
     * Constructor to apply only parameter changes without an ambience event.
     * </summary>
     */
    public ApplyAmbienceEvent(AudioParamValue[] parameters)
    {
        AmbienceEvent = null;
        Parameters = parameters;
    }
}

/**
 * <summary>
 * Event to remove ambient sounds.
 * </summary>
 */
public struct RemoveAmbienceEvent : IEvent
{
    public readonly bool FadeOut;
    
    public RemoveAmbienceEvent(bool fadeOut = true)
    {
        FadeOut = fadeOut;
    }
}

#endregion

#region Snapshot Events

/**
 * <summary>
 * Event to apply an audio snapshot with a specified fade time.
 * </summary>
 */
public struct ApplySnapshotEvent : IEvent
{
    public readonly AudioSnapshot Snapshot;
    public readonly AudioParamValue[] Parameters;
    
    /** <summary>
     * Constructor to apply a snapshot with optional parameter changes.
     * </summary>
     */
    public ApplySnapshotEvent(AudioSnapshot snapshot, AudioParamValue[] parameters = null)
    {
        Snapshot = snapshot;
        Parameters = parameters;
    }
}

/**
 * <summary>
 * Event to remove an audio snapshot with a specified fade time.
 * </summary>
 */
public struct RemoveSnapshotEvent : IEvent
{
    public readonly AudioSnapshot[] Snapshots;
    public readonly bool RemoveAll;
    
    /** <summary>
     * Constructor to remove a specific snapshot.
     * </summary>
     */
    public RemoveSnapshotEvent(AudioSnapshot snapshot)
    {
        Snapshots = new AudioSnapshot[] { snapshot };
        RemoveAll = false;
    }
    
    /** <summary>
     * Constructor to remove multiple specific snapshots.
     * </summary>
     */
    public RemoveSnapshotEvent(AudioSnapshot[] snapshots)
    {
        Snapshots = snapshots;
        RemoveAll = false;
    }
    
    /** <summary>
     * Constructor to remove all snapshots.
     * </summary>
     */
    public RemoveSnapshotEvent(bool removeAll = true)
    {
        Snapshots = null;
        RemoveAll = removeAll;
    }
}

#endregion