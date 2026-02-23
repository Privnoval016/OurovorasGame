using UnityEngine;

/**
 * <summary>
 * A ScriptableObject that defines a group of scenes to be loaded/unloaded together additively.
 * A group typically represents one "level" or "area" composed of multiple scene partitions
 * (e.g. gameplay, lighting, audio, streaming regions).
 *
 * A single <see cref="SceneReference"/> in a group may optionally be designated as the
 * <em>active</em> scene — this is the scene that receives newly instantiated objects when
 * none is specified (see <see cref="ActiveSceneIndex"/>).
 *
 * All load/unload operations are executed via <see cref="SceneLoader"/>.
 * </summary>
 */
[CreateAssetMenu(menuName = "Scene Management/Scene Group")]
public class SceneGroup : ScriptableObject
{
    /**
     * <summary>
     * The ordered list of <see cref="SceneReference"/>s that belong to this group.
     * All scenes are loaded additively when this group is requested.
     * </summary>
     */
    [SerializeField] private SceneReference[] scenes = System.Array.Empty<SceneReference>();

    /**
     * <summary>
     * Index into <see cref="scenes"/> for the scene that should be set as the active scene
     * after the group loads. Defaults to 0.
     * </summary>
     */
    [SerializeField, Tooltip("Index of the scene within this group that will be set as the active scene after loading.")]
    private int activeSceneIndex = 0;

    /** <summary>Read-only view of the scene references in this group.</summary> */
    public System.ReadOnlySpan<SceneReference> Scenes => scenes;

    /**
     * <summary>
     * The <see cref="SceneReference"/> that should be made the active scene after loading.
     * Falls back to the first scene if the index is out of range.
     * </summary>
     */
    public SceneReference ActiveScene
    {
        get
        {
            if (scenes == null || scenes.Length == 0) return null;
            int idx = Mathf.Clamp(activeSceneIndex, 0, scenes.Length - 1);
            return scenes[idx];
        }
    }

    /** <summary>Returns true if this group has at least one valid scene reference.</summary> */
    public bool IsValid
    {
        get
        {
            if (scenes == null || scenes.Length == 0) return false;
            foreach (var s in scenes)
                if (s != null && s.IsValid) return true;
            return false;
        }
    }

    /**
     * <summary>
     * Convenience method — requests <see cref="SceneLoader"/> to load this group additively.
     * The caller must ensure a <see cref="SceneLoader"/> instance exists in the scene.
     * </summary>
     */
    public void LoadAdditive() => SceneLoader.Instance.LoadGroupAdditive(this);

    /**
     * <summary>
     * Convenience method — requests <see cref="SceneLoader"/> to unload this group.
     * The caller must ensure a <see cref="SceneLoader"/> instance exists in the scene.
     * </summary>
     */
    public void Unload() => SceneLoader.Instance.UnloadGroup(this);
}