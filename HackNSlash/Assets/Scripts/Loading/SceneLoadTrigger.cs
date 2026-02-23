using UnityEngine;

/**
 * <summary>
 * A physics trigger that requests scene group loads and unloads from <see cref="SceneLoader"/>
 * when the player enters or exits its collider volume.
 *
 * This is the primary way to stream content in and out as the player moves through the world.
 * Configure which group to load in <see cref="groupToLoad"/> and optionally which group to
 * unload (if any) in <see cref="groupToUnload"/>.
 *
 * The trigger only responds to objects tagged <c>"Player"</c> by default; override
 * <see cref="IsValidTriggerTarget"/> to change this behaviour.
 * </summary>
 */
[RequireComponent(typeof(Collider))]
public class SceneLoadTrigger : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    #region Inspector Fields

    [Header("Load Settings")]
    [Tooltip("The scene group to load additively when the player enters this trigger.")]
    [SerializeField] private SceneGroup groupToLoad;

    [Tooltip("Optional scene group to unload when the player enters this trigger " +
             "(e.g. the previous area the player is leaving).")]
    [SerializeField] private SceneGroup groupToUnload;

    [Header("Unload On Exit")]
    [Tooltip("If true, the loaded group will be unloaded when the player exits this trigger. " +
             "Set false for one-way streaming gates.")]
    [SerializeField] private bool unloadOnExit = false;

    [Header("Transition Mode")]
    [Tooltip("If true, uses TransitionToGroup which shows a loading screen while swapping " +
             "groupToUnload for groupToLoad.  If false, loads/unloads independently.")]
    [SerializeField] private bool useTransition = false;

    [Tooltip("Tag used to identify the player object that activates this trigger.")]
    [SerializeField] private string playerTag = "Player";

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Trigger Callbacks

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidTriggerTarget(other)) return;

        if (groupToLoad == null) return;

        if (useTransition)
        {
            SceneLoader.Instance.TransitionToGroup(groupToLoad, groupToUnload);
        }
        else
        {
            if (groupToUnload != null)
                SceneLoader.Instance.UnloadGroup(groupToUnload);

            SceneLoader.Instance.LoadGroupAdditive(groupToLoad);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!unloadOnExit) return;
        if (!IsValidTriggerTarget(other)) return;

        if (groupToLoad != null)
            SceneLoader.Instance.UnloadGroup(groupToLoad);
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Overridable

    /**
     * <summary>
     * Returns true if <paramref name="other"/> is a valid object for triggering a scene transition.
     * Default implementation checks the object's tag against <see cref="playerTag"/>.
     * Override in a subclass to implement custom detection (e.g. checking for a specific component).
     * </summary>
     * <param name="other">The collider that entered or exited this trigger volume.</param>
     */
    protected virtual bool IsValidTriggerTarget(Collider other) =>
        other.CompareTag(playerTag);

    #endregion
}