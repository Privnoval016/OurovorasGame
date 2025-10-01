using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Extensions.EventBus
{
    /**
     * <summary>
     * Utility class for managing EventBus types and their lifecycle. It initializes EventBus instances for all event types
     * found in the predefined assemblies and provides methods to clear them when necessary.
     * </summary>
     */
    public static class EventBusUtil
    {
        public static IReadOnlyList<Type> EventTypes { get; set; }
        public static IReadOnlyList<Type> EventBusTypes { get; set; }
        
#if UNITY_EDITOR
        public static PlayModeStateChange PlayModeState { get; set; }

        /**
         * <summary>
         * Initializes the editor by subscribing to play mode state changes.
         * This method is called automatically when the Unity editor loads.
         * </summary>
         */
        [InitializeOnLoadMethod]
        public static void InitializeEditor()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /**
         * <summary>
         * Handles play mode state changes in the Unity editor.
         * When exiting play mode, it clears all EventBus instances to reset their state.
         * </summary>
         */
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            PlayModeState = state;
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ClearAllBuses();
            }
        }
#endif
        
        /**
         * <summary>
         * Initializes the EventBus system by discovering all event types and creating corresponding EventBus instances.
         * This method is called automatically before any scene is loaded.
         * </summary>
         */
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            EventTypes = PredefinedAssemblyUtil.GetTypes(typeof(IEvent));
            EventBusTypes = InitializeAllBuses();
        }

        /**
         * <summary>
         * Creates EventBus instances for all discovered event types.
         * Returns a list of the created EventBus types.
         * </summary>
         *
         * <returns>List of EventBus types corresponding to each event type.</returns>
         */
        private static List<Type> InitializeAllBuses()
        {
            List<Type> eventBusTypes = new();
            
            var typedef = typeof(EventBus<>);
            
            foreach (var eventType in EventTypes)
            {
                var constructedType = typedef.MakeGenericType(eventType);
                eventBusTypes.Add(constructedType);
                Debug.Log($"Initialized EventBus for event type: {eventType.Name}");
            }
            
            return eventBusTypes;
        }
        
        /**
         * <summary>
         * Clears all EventBus instances by invoking their static Clear method.
         * This is useful for resetting the state of the EventBus system, especially when exiting play mode in the editor.
         * </summary>
         */
        public static void ClearAllBuses()
        {
            Debug.Log("Clearing all EventBus types");
            
            foreach (var busType in EventBusTypes)
            {
                var clearMethod = busType.GetMethod("Clear", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                clearMethod?.Invoke(null, null);
                Debug.Log($"Cleared EventBus for type: {busType.Name}");
            }
        }
    }
}