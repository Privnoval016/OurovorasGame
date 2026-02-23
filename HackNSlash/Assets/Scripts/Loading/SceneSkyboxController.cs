using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/**
 * <summary>
 * Placed in any scene that wants to supply a specific skybox material when that scene is
 * active or loaded.
 *
 * On <c>OnEnable</c> this component registers itself with <see cref="SkyboxManager"/>, which
 * immediately reconciles and may apply this skybox. On <c>OnDisable</c> it deregisters itself
 * so the next-best registered skybox takes over.
 *
 * Only one <see cref="SceneSkyboxController"/> per scene is supported. If you place multiple
 * in the same scene, the last one to enable wins within that scene's slot.
 *
 * <para>
 * In the editor, use the <em>Preview Skybox In Editor</em> button to immediately apply this
 * skybox to <see cref="RenderSettings"/> without entering play mode. This is especially useful
 * when you drag a scene in additively and want to see how it looks.
 * </para>
 * </summary>
 */
public class SceneSkyboxController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    #region Inspector Fields

    [Header("Skybox")]
    [Tooltip("The skybox material to use when this scene is the active or highest-priority scene. " +
             "Leave null to have no effect.")]
    [SerializeField] private Material skyboxMaterial;

    [Tooltip("Tiebreaker priority used when multiple scenes are loaded simultaneously and none is the " +
             "Unity active scene. Higher values take precedence. Has no effect when this scene is the " +
             "active scene, which always wins.")]
    [SerializeField] private int priority;

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Properties

    /** <summary>The skybox material this scene contributes. May be null.</summary> */
    public Material SkyboxMaterial => skyboxMaterial;

    /**
     * <summary>
     * Tiebreaker priority. When the Unity active scene has no controller, the registered
     * controller with the highest priority wins.
     * </summary>
     */
    public int Priority => priority;

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void OnEnable()
    {
        if (skyboxMaterial == null) return;
        SkyboxManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        // Deregister even if material is null — guard is inside SkyboxManager
        if (SkyboxManager.HasInstance)
            SkyboxManager.Instance.Deregister(this);
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Editor Preview

#if UNITY_EDITOR
    /**
     * <summary>
     * Immediately applies this scene's skybox material to <see cref="RenderSettings"/> in the
     * editor without entering play mode.
     *
     * Use this after dragging a scene in additively to preview how the skybox looks.
     * The change is applied to <see cref="RenderSettings.skybox"/> in the currently open set of
     * scenes and <see cref="DynamicGI.UpdateEnvironment"/> is called so GI probes update.
     * The scene containing this component is marked dirty so the preview can be undone (Ctrl+Z).
     * </summary>
     */
    [Button("Preview Skybox In Editor"), PropertySpace(4)]
    [InfoBox("Applies this skybox to RenderSettings immediately in the editor. " +
             "Works when the scene is loaded additively — no play mode required.",
             InfoMessageType.Info)]
    private void PreviewSkyboxInEditor()
    {
        if (skyboxMaterial == null)
        {
            Debug.LogWarning($"[SceneSkyboxController] No skybox material assigned on '{gameObject.scene.name}'.");
            return;
        }

        // RenderSettings is a static class, not a UnityEngine.Object, but its backing asset
        // can be retrieved from ProjectSettings. We use SerializedObject on it so that Unity's
        // undo system tracks the change and Ctrl+Z works — the same approach Unity's Lighting
        // window uses internally.
        UnityEngine.Object[] rsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/RenderSettings.asset");
        if (rsAssets != null && rsAssets.Length > 0)
        {
            var so = new SerializedObject(rsAssets[0]);
            so.FindProperty("m_SkyboxMaterial").objectReferenceValue = skyboxMaterial;
            so.ApplyModifiedProperties();
        }
        else
        {
            // Fallback: direct assignment (no undo support, but functional)
            RenderSettings.skybox = skyboxMaterial;
        }

        DynamicGI.UpdateEnvironment();
        EditorSceneManager.MarkSceneDirty(gameObject.scene);

        Debug.Log($"[SceneSkyboxController] Previewing skybox from scene '{gameObject.scene.name}': {skyboxMaterial.name}");
    }

    /**
     * <summary>
     * Reverts <see cref="RenderSettings.skybox"/> to whatever was set before the last preview,
     * using Unity's undo system. Equivalent to pressing Ctrl+Z after <see cref="PreviewSkyboxInEditor"/>.
     * </summary>
     */
    [Button("Revert Skybox (Undo)"), PropertySpace(2)]
    private void RevertSkybox()
    {
        Undo.PerformUndo();
        DynamicGI.UpdateEnvironment();
    }
#endif

    #endregion
}

