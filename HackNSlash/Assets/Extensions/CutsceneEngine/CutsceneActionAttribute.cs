using System;

namespace Extensions.CutsceneEngine
{
    /**
     * <summary>
     * The CutsceneActionAttribute is a custom attribute used to mark methods as cutscene actions. It provides metadata about the action, such as its display name and whether it can be called during gameplay or only in cinematic contexts.
     * Methods decorated with this attribute can be invoked as part of cutscene sequences, allowing for organized and easily identifiable cutscene actions within the codebase.
     * </summary>
     */
    [AttributeUsage(AttributeTargets.Method)]
    public class CutsceneActionAttribute : Attribute
    {
        /**
         * <summary>
         * The display name of the cutscene action, which can be used for UI representation or debugging purposes.
         * </summary>
         */
        public string DisplayName;
        
        /**
         * <summary>
         * Indicates whether this cutscene action can be called during regular gameplay. If false, the action can only be invoked within cinematic contexts.
         * </summary>
         */
        public bool AllowDuringGameplay;
        
        /**
         * <summary>
         * Indicates whether this cutscene action is intended to be used only in cinematic contexts. If true, the action should not be called during regular gameplay.
         * </summary>
         */
        public bool CinematicOnly;

        
        public CutsceneActionAttribute(string displayName, bool allowDuringGameplay = true, bool cinematicOnly = false)
        {
            DisplayName = displayName;
            AllowDuringGameplay = allowDuringGameplay;
            CinematicOnly = cinematicOnly;
        }
    }

}