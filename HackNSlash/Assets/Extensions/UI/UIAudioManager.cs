using Extensions.EventBus;
using FMODUnity;
using UnityEngine;

namespace Extensions.UI
{
    /// <summary>
    /// UI audio event types for FMOD playback through AudioSystem.
    /// Uses EventBus to avoid singleton pattern and global access issues.
    /// </summary>
    public enum UIAudioType
    {
        Navigation,     // Moving between menu items
        Select,         // Confirming/clicking
        Back,           // Canceling/closing
        TabSwitch,      // Switching main tabs
        Error,          // Invalid action
        Hover,          // Hovering over button
        Unlock,         // Unlocking skill/achievement
        SlotSelect,     // Selecting equipment slot
        ItemEquip       // Equipping an item
    }
    
    /// <summary>
    /// Event for requesting UI audio playback.
    /// Consumed by AudioSystem to play FMOD events.
    /// </summary>
    public struct PlayUIAudioEvent : IEvent
    {
        public UIAudioType audioType;
        public EventReference customEvent; // Optional custom FMOD event
        
        public PlayUIAudioEvent(UIAudioType type)
        {
            audioType = type;
            customEvent = default;
        }
        
        public PlayUIAudioEvent(EventReference fmodEvent)
        {
            audioType = UIAudioType.Select; // Default
            customEvent = fmodEvent;
        }
    }
    
    /// <summary>
    /// Static helper for raising UI audio events.
    /// No singleton - uses EventBus for decoupled communication.
    /// </summary>
    public static class UIAudio
    {
        /// <summary>
        /// Plays navigation sound (moving between menu items).
        /// </summary>
        public static void PlayNavigation()
        {
            EventBus<PlayUIAudioEvent>.Raise(new PlayUIAudioEvent(UIAudioType.Navigation));
        }
        
        /// <summary>
        /// Plays select/confirm sound (clicking a button, selecting an item).
        /// </summary>
        public static void PlaySelect()
        {
            EventBus<PlayUIAudioEvent>.Raise(new PlayUIAudioEvent(UIAudioType.Select));
        }
        
        /// <summary>
        /// Plays back/cancel sound (closing a menu, canceling an action).
        /// </summary>
        public static void PlayBack()
        {
            EventBus<PlayUIAudioEvent>.Raise(new PlayUIAudioEvent(UIAudioType.Back));
        }
        
        /// <summary>
        /// Plays tab switch sound (switching between main menu tabs).
        /// </summary>
        public static void PlayTabSwitch()
        {
            EventBus<PlayUIAudioEvent>.Raise(new PlayUIAudioEvent(UIAudioType.TabSwitch));
        }
        
        /// <summary>
        /// Plays error sound (invalid action, locked item).
        /// </summary>
        public static void PlayError()
        {
            EventBus<PlayUIAudioEvent>.Raise(new PlayUIAudioEvent(UIAudioType.Error));
        }
        
        /// <summary>
        /// Plays hover sound (mouse/controller hovering over button).
        /// </summary>
        public static void PlayHover()
        {
            EventBus<PlayUIAudioEvent>.Raise(new PlayUIAudioEvent(UIAudioType.Hover));
        }
        
        /// <summary>
        /// Plays unlock sound (unlocking a skill, achievement popup).
        /// </summary>
        public static void PlayUnlock()
        {
            EventBus<PlayUIAudioEvent>.Raise(new PlayUIAudioEvent(UIAudioType.Unlock));
        }
        
        /// <summary>
        /// Plays slot selection sound (clicking an equipment slot).
        /// </summary>
        public static void PlaySlotSelect()
        {
            EventBus<PlayUIAudioEvent>.Raise(new PlayUIAudioEvent(UIAudioType.SlotSelect));
        }
        
        /// <summary>
        /// Plays item equip sound (equipping an item).
        /// </summary>
        public static void PlayItemEquip()
        {
            EventBus<PlayUIAudioEvent>.Raise(new PlayUIAudioEvent(UIAudioType.ItemEquip));
        }
        
        /// <summary>
        /// Plays a custom FMOD event.
        /// </summary>
        /// <param name="eventReference">The FMOD event to play.</param>
        public static void PlayCustom(EventReference eventReference)
        {
            EventBus<PlayUIAudioEvent>.Raise(new PlayUIAudioEvent(eventReference));
        }
    }
}

