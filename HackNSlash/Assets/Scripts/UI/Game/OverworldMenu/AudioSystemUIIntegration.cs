using Extensions.EventBus;
using Extensions.UI;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Example integration of UI audio events with AudioSystem.
/// Add this listener to your existing AudioSystem class.
/// </summary>
public class AudioSystemUIIntegration : MonoBehaviour
{
    [Header("UI Audio FMOD Events")]
    [Tooltip("Assign FMOD events in Inspector")]
    [SerializeField] private EventReference uiNavigationEvent;
    [SerializeField] private EventReference uiSelectEvent;
    [SerializeField] private EventReference uiBackEvent;
    [SerializeField] private EventReference uiTabSwitchEvent;
    [SerializeField] private EventReference uiErrorEvent;
    [SerializeField] private EventReference uiHoverEvent;
    [SerializeField] private EventReference uiUnlockEvent;
    [SerializeField] private EventReference uiSlotSelectEvent;
    [SerializeField] private EventReference uiItemEquipEvent;
    
    [Header("Settings")]
    [SerializeField] private float hoverSoundCooldown = 0.1f;
    
    private EventBinding<PlayUIAudioEvent> uiAudioBinding;
    private float lastHoverTime;
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        // Subscribe to UI audio events via EventBus
        uiAudioBinding = new EventBinding<PlayUIAudioEvent>(OnPlayUIAudio);
        EventBus<PlayUIAudioEvent>.Register(uiAudioBinding);
    }
    
    private void OnDestroy()
    {
        // Clean up subscription
        if (uiAudioBinding != null)
            EventBus<PlayUIAudioEvent>.Deregister(uiAudioBinding);
    }
    
    #endregion
    
    #region Event Handlers
    
    /// <summary>
    /// Handles UI audio event requests and plays appropriate FMOD sounds.
    /// </summary>
    /// <param name="evt">The UI audio event.</param>
    private void OnPlayUIAudio(PlayUIAudioEvent evt)
    {
        // If custom FMOD event specified, play it directly
        if (!evt.customEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(evt.customEvent);
            return;
        }
        
        // Map UIAudioType to FMOD event
        EventReference eventToPlay = GetFMODEventForType(evt.audioType);
        
        // Special handling for hover to prevent spam
        if (evt.audioType == UIAudioType.Hover)
        {
            if (Time.unscaledTime - lastHoverTime < hoverSoundCooldown)
                return;
            
            lastHoverTime = Time.unscaledTime;
        }
        
        // Play the FMOD event
        if (!eventToPlay.IsNull)
        {
            RuntimeManager.PlayOneShot(eventToPlay);
        }
        else
        {
            Debug.LogWarning($"AudioSystemUIIntegration: No FMOD event assigned for {evt.audioType}");
        }
    }
    
    /// <summary>
    /// Maps UI audio type to FMOD event reference.
    /// </summary>
    private EventReference GetFMODEventForType(UIAudioType audioType)
    {
        return audioType switch
        {
            UIAudioType.Navigation => uiNavigationEvent,
            UIAudioType.Select => uiSelectEvent,
            UIAudioType.Back => uiBackEvent,
            UIAudioType.TabSwitch => uiTabSwitchEvent,
            UIAudioType.Error => uiErrorEvent,
            UIAudioType.Hover => uiHoverEvent,
            UIAudioType.Unlock => uiUnlockEvent,
            UIAudioType.SlotSelect => uiSlotSelectEvent,
            UIAudioType.ItemEquip => uiItemEquipEvent,
            _ => default
        };
    }
    
    #endregion
}

/* ============================================================================
 * INTEGRATION INSTRUCTIONS
 * ============================================================================
 * 
 * Option 1: Add to Existing AudioSystem
 * --------------------------------------
 * If you have an AudioSystem class, copy the code above into it:
 * 
 * public class AudioSystem : MonoBehaviour
 * {
 *     // ...existing audio code...
 *     
 *     // Add the fields, Awake subscription, and handler from above
 *     [SerializeField] private EventReference uiNavigationEvent;
 *     // ... etc
 *     
 *     private EventBinding<PlayUIAudioEvent> uiAudioBinding;
 *     
 *     private void Awake()
 *     {
 *         // ...existing code...
 *         
 *         // Add UI audio subscription
 *         uiAudioBinding = new EventBinding<PlayUIAudioEvent>(OnPlayUIAudio);
 *         EventBus<PlayUIAudioEvent>.Register(uiAudioBinding);
 *     }
 *     
 *     private void OnPlayUIAudio(PlayUIAudioEvent evt) { ... }
 * }
 * 
 * 
 * Option 2: Use as Separate Component
 * ------------------------------------
 * 1. Add AudioSystemUIIntegration component to your AudioSystem GameObject
 * 2. Assign FMOD events in Inspector
 * 3. Done!
 * 
 * 
 * FMOD Event Setup
 * ----------------
 * Create these events in FMOD Studio:
 * 
 * event:/UI/Navigation     - Short blip for menu navigation
 * event:/UI/Select         - Satisfying click for confirmations
 * event:/UI/Back           - Softer sound for canceling
 * event:/UI/TabSwitch      - Distinct sound for tab changes
 * event:/UI/Error          - Harsh/dissonant for errors
 * event:/UI/Hover          - Very subtle for hover feedback
 * event:/UI/Unlock         - Rewarding chime for unlocks
 * event:/UI/SlotSelect     - Click for selecting equipment slots
 * event:/UI/ItemEquip      - Satisfying sound for equipping items
 * 
 * Tips:
 * - Keep UI sounds short (50-200ms)
 * - Navigation should be quiet and unobtrusive
 * - Select should feel satisfying and definitive
 * - Error should be noticeably different (negative feedback)
 * - Consider using parameter variations for same event type
 * 
 * ============================================================================
 */

