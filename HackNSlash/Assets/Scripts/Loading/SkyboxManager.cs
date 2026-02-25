using System.Collections.Generic;
using Extensions.Patterns;
using UnityEngine;
using UnityEngine.SceneManagement;

/**
 * <summary>
 * Manages which skybox material is applied to <see cref="RenderSettings"/> at any given time.
 *
 * Each loaded scene that wants to supply its own skybox places a <see cref="SceneSkyboxController"/>
 * component anywhere in that scene. When a scene becomes active (or when scenes are loaded/unloaded),
 * <see cref="Reconcile"/> is called to determine the winning skybox.
 *
 * <para><strong>Selection order:</strong></para>
 * <list type="number">
 *   <item>The skybox belonging to the scene set as the Unity active scene, provided that scene is
 *         not in the <em>excluded</em> set (see <see cref="ExcludeScene"/>).</item>
 *   <item>The registered controller with the highest <see cref="SceneSkyboxController.Priority"/>
 *         value, again skipping excluded scenes.</item>
 *   <item>Nothing — the current skybox is left unchanged if no eligible controller is found.</item>
 * </list>
 *
 * <para>
 * The bootstrap / player scene should be excluded so that level-additive scenes always drive the
 * skybox. Call <see cref="ExcludeScene"/> from <see cref="SceneLoader"/> during its <c>Awake</c>.
 * </para>
 *
 * <para>
 * This class is a <see cref="PersistentSingleton{T}"/> and lives in the bootstrap scene for the
 * lifetime of the application. Only <see cref="SceneSkyboxController"/>s need to be placed in
 * individual scenes.
 * </para>
 * </summary>
 */
public class SkyboxManager : PersistentSingleton<SkyboxManager>
{
    
    #region Private State

    /**
     * <summary>
     * All currently registered <see cref="SceneSkyboxController"/>s, keyed by their owning
     * scene's build index. A scene may only have one active controller at a time.
     * </summary>
     */
    private readonly Dictionary<int, SceneSkyboxController> _registry = new();

    /**
     * <summary>
     * Build indices of scenes that are never allowed to win reconciliation.
     * Populated at runtime by <see cref="ExcludeScene"/>.
     * </summary>
     */
    private readonly HashSet<int> _excludedBuildIndices = new();

    #endregion
    
    #region Exclusion API

    /**
     * <summary>
     * Marks a scene as excluded so its <see cref="SceneSkyboxController"/> is never selected
     * during reconciliation, even if that scene is the Unity active scene.
     *
     * Call this from <see cref="SceneLoader"/> for the main/bootstrap scene so that
     * level-additive scenes always drive the skybox.
     * </summary>
     * <param name="scene">
     * A <see cref="SceneReference"/> identifying the scene to exclude.
     * Must correspond to a scene that is already in Build Settings.
     * </param>
     */
    public void ExcludeScene(SceneReference scene)
    {
        if (scene == null || !scene.IsValid) return;

        Scene unityScene = SceneManager.GetSceneByName(scene.SceneName);

        // If the scene isn't loaded yet we can't resolve its build index via SceneManager.
        // Use SceneUtility which works regardless of whether the scene is loaded.
        int buildIndex = unityScene.IsValid()
            ? unityScene.buildIndex
            : UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(
                  GetScenePath(scene));

        if (buildIndex < 0)
        {
            Debug.LogWarning($"[SkyboxManager] Could not resolve build index for excluded scene " +
                             $"'{scene.SceneName}'. Ensure it is in Build Settings.");
            return;
        }

        _excludedBuildIndices.Add(buildIndex);
    }

    /**
     * <summary>
     * Removes a scene from the exclusion list so it can participate in reconciliation again.
     * Useful for title-screen or rendered-cutscene scenes that should temporarily own the skybox.
     * </summary>
     * <param name="scene">The <see cref="SceneReference"/> to un-exclude.</param>
     */
    public void UnexcludeScene(SceneReference scene)
    {
        if (scene == null || !scene.IsValid) return;

        Scene unityScene = SceneManager.GetSceneByName(scene.SceneName);
        int buildIndex = unityScene.IsValid()
            ? unityScene.buildIndex
            : UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(
                  GetScenePath(scene));

        if (buildIndex >= 0)
            _excludedBuildIndices.Remove(buildIndex);
    }

    /**
     * <summary>
     * Returns true if the scene with the given build index is excluded from skybox selection.
     * </summary>
     */
    public bool IsExcluded(int buildIndex) => _excludedBuildIndices.Contains(buildIndex);

    #endregion
    
    #region Registration

    /**
     * <summary>
     * Registers a <see cref="SceneSkyboxController"/> so it participates in reconciliation.
     * Called automatically by <see cref="SceneSkyboxController.OnEnable"/>.
     * If the scene already has a registered controller it will be replaced.
     * </summary>
     * <param name="controller">The controller to register.</param>
     */
    public void Register(SceneSkyboxController controller)
    {
        if (controller == null) return;
        int buildIndex = controller.gameObject.scene.buildIndex;
        _registry[buildIndex] = controller;
        Reconcile();
    }

    /**
     * <summary>
     * Removes a previously registered <see cref="SceneSkyboxController"/>.
     * Called automatically by <see cref="SceneSkyboxController.OnDisable"/>.
     * Triggers a reconciliation so the next-best skybox takes over immediately.
     * </summary>
     * <param name="controller">The controller to deregister.</param>
     */
    public void Deregister(SceneSkyboxController controller)
    {
        if (controller == null) return;
        int buildIndex = controller.gameObject.scene.buildIndex;

        if (_registry.TryGetValue(buildIndex, out var existing) && existing == controller)
        {
            _registry.Remove(buildIndex);
            Reconcile();
        }
    }

    #endregion
    
    #region Reconciliation

    /**
     * <summary>
     * Evaluates all registered <see cref="SceneSkyboxController"/>s and applies the winning
     * skybox material to <see cref="RenderSettings.skybox"/>.
     *
     * Excluded scenes (e.g. the bootstrap player scene) are always skipped.
     *
     * Selection order:
     * <list type="number">
     *   <item>Controller whose scene is the Unity active scene and is not excluded.</item>
     *   <item>Controller with the highest <see cref="SceneSkyboxController.Priority"/>, not excluded.</item>
     *   <item>No change if no eligible controller exists.</item>
     * </list>
     *
     * Called automatically after every register/deregister, and by <see cref="SceneLoader"/>
     * after every load/unload operation.
     * </summary>
     */
    public void Reconcile()
    {
        if (_registry.Count == 0) return;

        Scene activeScene = SceneManager.GetActiveScene();
        int activeBuildIndex = activeScene.buildIndex;

        // 1. Active scene wins — unless it is excluded
        if (!_excludedBuildIndices.Contains(activeBuildIndex)
            && _registry.TryGetValue(activeBuildIndex, out SceneSkyboxController activeController)
            && activeController != null
            && activeController.SkyboxMaterial != null)
        {
            Apply(activeController.SkyboxMaterial);
            return;
        }

        // 2. Highest-priority non-excluded controller wins
        SceneSkyboxController winner = null;
        foreach (var kvp in _registry)
        {
            if (_excludedBuildIndices.Contains(kvp.Key)) continue;

            var candidate = kvp.Value;
            if (candidate == null || candidate.SkyboxMaterial == null) continue;
            if (winner == null || candidate.Priority > winner.Priority)
                winner = candidate;
        }

        if (winner != null)
            Apply(winner.SkyboxMaterial);
    }

    #endregion
    
    #region Private Helpers

    private static void Apply(Material skybox)
    {
        if (RenderSettings.skybox == skybox) return;
        RenderSettings.skybox = skybox;
        DynamicGI.UpdateEnvironment();
    }

    /**
     * <summary>
     * Returns the asset path for a <see cref="SceneReference"/>.
     * In editor builds the path comes from <c>ScenePath</c>; in player builds it falls back
     * to a search through registered build indices by name.
     * </summary>
     */
    private static string GetScenePath(SceneReference scene)
    {
#if UNITY_EDITOR
        return scene.ScenePath;
#else
        // In a build, SceneUtility.GetBuildIndexByScenePath expects the full path as stored
        // in build settings (e.g. "Assets/Scenes/Main.unity"). Since we only have the name at
        // runtime, iterate all build scenes to find a match.
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == scene.SceneName)
                return path;
        }
        return string.Empty;
#endif
    }

    #endregion
}

