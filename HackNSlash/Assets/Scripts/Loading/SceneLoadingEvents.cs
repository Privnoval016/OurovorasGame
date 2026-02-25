using Extensions.EventBus;

/**
 * <summary>
 * Contains all EventBus events related to scene loading lifecycle.
 * Subscribe to these via <see cref="EventBus{T}"/> to react to scene transitions
 * without creating hard dependencies on <see cref="SceneLoader"/>.
 * </summary>
 */
public static class SceneLoadingEvents
{
    /**
     * <summary>
     * Fired immediately before a scene (or scene group) begins loading.
     * Use this to show a loading screen or pause gameplay systems.
     * </summary>
     */
    public struct OnSceneLoadStarted : IEvent
    {
        /** <summary>The name of the scene being loaded. Null for group loads.</summary> */
        public string SceneName;

        /** <summary>The group being loaded. Null for single-scene loads.</summary> */
        public SceneGroup Group;

        /** <summary>True when this is an additive load; false when it replaces the current scene.</summary> */
        public bool IsAdditive;
    }

    /**
     * <summary>
     * Fired after a scene (or scene group) finishes loading and all scenes are active.
     * Use this to initialize content in the newly loaded scene.
     * </summary>
     */
    public struct OnSceneLoadCompleted : IEvent
    {
        /** <summary>The name of the scene that finished loading. Null for group loads.</summary> */
        public string SceneName;

        /** <summary>The group that finished loading. Null for single-scene loads.</summary> */
        public SceneGroup Group;
    }

    /**
     * <summary>
     * Fired immediately before a scene (or scene group) begins unloading.
     * </summary>
     */
    public struct OnSceneUnloadStarted : IEvent
    {
        /** <summary>The name of the scene being unloaded. Null for group unloads.</summary> */
        public string SceneName;

        /** <summary>The group being unloaded. Null for single-scene unloads.</summary> */
        public SceneGroup Group;
    }

    /**
     * <summary>
     * Fired after a scene (or scene group) has been fully unloaded.
     * </summary>
     */
    public struct OnSceneUnloadCompleted : IEvent
    {
        /** <summary>The name of the scene that was unloaded. Null for group unloads.</summary> */
        public string SceneName;

        /** <summary>The group that was unloaded. Null for single-scene unloads.</summary> */
        public SceneGroup Group;
    }

    /**
     * <summary>
     * Fired each frame during a load operation with a normalised [0,1] progress value.
     * Useful for driving loading bars.
     * </summary>
     */
    public struct OnSceneLoadProgress : IEvent
    {
        /** <summary>Normalised load progress in the range [0, 1].</summary> */
        public float Progress;
    }
}

