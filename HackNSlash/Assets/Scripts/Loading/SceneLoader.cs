using System.Collections.Generic;
using Extensions.EventBus;
using Extensions.Patterns;
using Extensions.Utils;
using MEC;
using UnityEngine;
using UnityEngine.SceneManagement;

/**
 * <summary>
 * Central scene loading pipeline for the game.
 *
 * Responsibilities:
 * <list type="bullet">
 *   <item>All scene load and unload operations must go through this class.</item>
 *   <item>Tracks every currently loaded scene by name so callers can query state without polling Unity's SceneManager directly.</item>
 *   <item>Supports a single persistent "main" scene (the bootstrap/player scene) that is loaded once and never unloaded.</item>
 *   <item>Supports additively loading and unloading individual <see cref="SceneReference"/>s and full <see cref="SceneGroup"/>s.</item>
 *   <item>Prevents duplicate loads and redundant unloads.</item>
 *   <item>Broadcasts lifecycle events via the <see cref="EventBus{T}"/> so other systems can react without coupling to this class.</item>
 *   <item>Uses MEC (More Efficient Coroutines) for all async work so it integrates with the rest of the project.</item>
 * </list>
 *
 * Setup:
 * <list type="number">
 *   <item>Place one instance of this MonoBehaviour in your bootstrap/initialisation scene.</item>
 *   <item>Assign <see cref="mainScene"/> to the <see cref="SceneReference"/> that represents the player scene (or
 *         whatever persistent scene should never be unloaded).</item>
 *   <item>Optionally assign <see cref="loadingScreenScene"/> to show a dedicated loading screen during transitions.</item>
 *   <item>Call <see cref="LoadMainScene"/> once at startup (e.g. from the title screen) to bring in the player scene.</item>
 * </list>
 * </summary>
 */
public class SceneLoader : Singleton<SceneLoader>
{
    #region Inspector Fields

    [Header("Persistent Scene")]
    [Tooltip("The scene that contains the player and all persistent game objects. " +
             "It is loaded once and never unloaded during a play session.")]
    [SerializeField] private SceneReference mainScene;

    [Header("Loading Screen")]
    [Tooltip("Optional scene to load additively as a loading screen during transitions. " +
             "Leave empty to skip.")]
    [SerializeField] private SceneReference loadingScreenScene;

    [Tooltip("Minimum time (seconds) the loading screen is shown, even if loading finishes faster. " +
             "Prevents jarring flashes.")]
    [SerializeField, Min(0f)] private float minimumLoadingScreenDuration = 0.5f;

    #endregion
    #region Private State

    /** <summary>Names of all scenes currently loaded (excluding the loading screen itself).</summary> */
    private readonly HashSet<string> _loadedScenes = new();

    /** <summary>Maps each loaded <see cref="SceneGroup"/> to the set of scene names it contributed.</summary> */
    private readonly Dictionary<SceneGroup, HashSet<string>> _loadedGroups = new();

    /** <summary>True while any load or unload operation is in progress — prevents re-entrant calls.</summary> */
    private bool _isOperationInProgress;

    private const string LoaderTag = "SceneLoader";

    #endregion
    
    #region Monobehaviour Callbacks

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);

        // Register the currently active scene (the bootstrap/title scene) so it is tracked.
        Scene active = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(active.name))
            _loadedScenes.Add(active.name);

        // Exclude the main (player/bootstrap) scene from skybox selection so that additive
        // level scenes always drive the skybox. Done here so the exclusion is applied before
        // any SceneSkyboxController in the main scene has a chance to register itself.
        if (mainScene != null && mainScene.IsValid)
            SkyboxManager.Instance.ExcludeScene(mainScene);
    }
    #endregion

    #region Public Queries

    /**
     * <summary>
     * Returns true if the scene identified by <paramref name="scene"/> is currently loaded.
     * </summary>
     * <param name="scene">A <see cref="SceneReference"/> identifying the scene to check.</param>
     */
    public bool IsLoaded(SceneReference scene) =>
        scene != null && scene.IsValid && _loadedScenes.Contains(scene.SceneName);

    /**
     * <summary>
     * Returns true if every scene in <paramref name="group"/> is currently loaded.
     * </summary>
     * <param name="group">The <see cref="SceneGroup"/> to check.</param>
     */
    public bool IsGroupLoaded(SceneGroup group) =>
        group != null && _loadedGroups.ContainsKey(group);

    #endregion

    #region Main Scene

    /**
     * <summary>
     * Loads the <see cref="mainScene"/> additively.
     * Should be called once from the title screen or bootstrap sequence.
     * Does nothing if the main scene is already loaded.
     * </summary>
     */
    public void LoadMainScene()
    {
        if (mainScene == null || !mainScene.IsValid)
        {
            Debug.LogError("[SceneLoader] mainScene is not assigned or invalid.");
            return;
        }

        if (IsLoaded(mainScene)) return;

        this.RunSegmentCoroutine(LoadSceneAdditiveRoutine(mainScene), LoaderTag, Segment.Update);
    }

    #endregion

    #region Single Scene Operations

    /**
     * <summary>
     * Additively loads a single scene identified by <paramref name="scene"/>.
     * If the scene is already loaded, this is a no-op.
     * </summary>
     * <param name="scene">A <see cref="SceneReference"/> pointing to the scene to load.</param>
     */
    public void LoadSceneAdditive(SceneReference scene)
    {
        if (!ValidateReference(scene)) return;
        if (IsLoaded(scene)) return;

        this.RunSegmentCoroutine(LoadSceneAdditiveRoutine(scene), LoaderTag, Segment.Update);
    }

    /**
     * <summary>
     * Unloads a single additively-loaded scene identified by <paramref name="scene"/>.
     * Does nothing if the scene is not currently loaded.
     * Refuses to unload the <see cref="mainScene"/>.
     * </summary>
     * <param name="scene">A <see cref="SceneReference"/> pointing to the scene to unload.</param>
     */
    public void UnloadScene(SceneReference scene)
    {
        if (!ValidateReference(scene)) return;
        if (!IsLoaded(scene)) return;
        if (scene == mainScene)
        {
            Debug.LogWarning("[SceneLoader] Cannot unload the main scene.");
            return;
        }

        this.RunSegmentCoroutine(UnloadSceneRoutine(scene), LoaderTag, Segment.Update);
    }

    #endregion

    #region Group Operations

    /**
     * <summary>
     * Additively loads all scenes in <paramref name="group"/>.
     * Scenes that are already loaded are skipped.
     * After all scenes are loaded the group's designated active scene is set as the active Unity scene.
     * </summary>
     * <param name="group">The <see cref="SceneGroup"/> to load.</param>
     */
    public void LoadGroupAdditive(SceneGroup group)
    {
        if (!ValidateGroup(group)) return;
        if (IsGroupLoaded(group)) return;

        this.RunSegmentCoroutine(LoadGroupRoutine(group, showLoadingScreen: true), LoaderTag, Segment.Update);
    }

    /**
     * <summary>
     * Unloads all scenes that were loaded as part of <paramref name="group"/>.
     * Does nothing if the group is not currently tracked as loaded.
     * Any individual scenes in the group that were also part of the main scene are left alone.
     * </summary>
     * <param name="group">The <see cref="SceneGroup"/> to unload.</param>
     */
    public void UnloadGroup(SceneGroup group)
    {
        if (!ValidateGroup(group)) return;
        if (!IsGroupLoaded(group)) return;

        this.RunSegmentCoroutine(UnloadGroupRoutine(group), LoaderTag, Segment.Update);
    }

    /**
     * <summary>
     * Replaces the currently loaded level group with a new one.
     * Unloads <paramref name="previous"/> (if provided and loaded) before loading <paramref name="next"/>.
     * Shows the loading screen during the transition.
     * </summary>
     * <param name="next">The <see cref="SceneGroup"/> to transition to.</param>
     * <param name="previous">Optional currently-loaded group to unload first.</param>
     */
    public void TransitionToGroup(SceneGroup next, SceneGroup previous = null)
    {
        if (!ValidateGroup(next)) return;

        this.RunSegmentCoroutine(TransitionGroupRoutine(next, previous), LoaderTag, Segment.Update);
    }

    #endregion

    #region MEC Coroutines

    private IEnumerator<float> LoadSceneAdditiveRoutine(SceneReference scene)
    {
        if (_isOperationInProgress)
        {
            Debug.LogWarning($"[SceneLoader] Load of '{scene.SceneName}' queued behind active operation.");
            yield return Timing.WaitUntilDone(
                Timing.RunCoroutine(WaitForOperationFree().CancelWith(this), Segment.Update));
        }

        _isOperationInProgress = true;

        EventBus<SceneLoadingEvents.OnSceneLoadStarted>.Raise(new SceneLoadingEvents.OnSceneLoadStarted
        {
            SceneName = scene.SceneName,
            IsAdditive = true
        });

        AsyncOperation op = SceneManager.LoadSceneAsync(scene.SceneName, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError($"[SceneLoader] Failed to start load for scene '{scene.SceneName}'. " +
                           "Ensure the scene is added to Build Settings.");
            _isOperationInProgress = false;
            yield break;
        }

        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            EventBus<SceneLoadingEvents.OnSceneLoadProgress>.Raise(
                new SceneLoadingEvents.OnSceneLoadProgress { Progress = op.progress });
            yield return Timing.WaitForOneFrame;
        }

        _loadedScenes.Add(scene.SceneName);

        // Reconcile skybox: the newly loaded scene may have a SceneSkyboxController
        if (SkyboxManager.HasInstance)
            SkyboxManager.Instance.Reconcile();

        EventBus<SceneLoadingEvents.OnSceneLoadCompleted>.Raise(new SceneLoadingEvents.OnSceneLoadCompleted
        {
            SceneName = scene.SceneName
        });

        _isOperationInProgress = false;
    }

    private IEnumerator<float> UnloadSceneRoutine(SceneReference scene)
    {
        if (_isOperationInProgress)
            yield return Timing.WaitUntilDone(
                Timing.RunCoroutine(WaitForOperationFree().CancelWith(this), Segment.Update));

        _isOperationInProgress = true;

        EventBus<SceneLoadingEvents.OnSceneUnloadStarted>.Raise(new SceneLoadingEvents.OnSceneUnloadStarted
        {
            SceneName = scene.SceneName
        });

        AsyncOperation op = SceneManager.UnloadSceneAsync(scene.SceneName);
        if (op == null)
        {
            Debug.LogError($"[SceneLoader] Failed to start unload for scene '{scene.SceneName}'.");
            _isOperationInProgress = false;
            yield break;
        }

        while (!op.isDone)
            yield return Timing.WaitForOneFrame;

        _loadedScenes.Remove(scene.SceneName);

        // Reconcile skybox: the unloaded scene's controller will have self-deregistered,
        // but call Reconcile explicitly here to handle any edge cases.
        if (SkyboxManager.HasInstance)
            SkyboxManager.Instance.Reconcile();

        EventBus<SceneLoadingEvents.OnSceneUnloadCompleted>.Raise(new SceneLoadingEvents.OnSceneUnloadCompleted
        {
            SceneName = scene.SceneName
        });

        _isOperationInProgress = false;
    }

    private IEnumerator<float> LoadGroupRoutine(SceneGroup group, bool showLoadingScreen)
    {
        if (_isOperationInProgress)
            yield return Timing.WaitUntilDone(
                Timing.RunCoroutine(WaitForOperationFree().CancelWith(this), Segment.Update));

        _isOperationInProgress = true;

        float loadStart = Time.unscaledTime;

        // Show loading screen
        if (showLoadingScreen)
            yield return Timing.WaitUntilDone(
                Timing.RunCoroutine(ShowLoadingScreen().CancelWith(this), Segment.Update));

        EventBus<SceneLoadingEvents.OnSceneLoadStarted>.Raise(new SceneLoadingEvents.OnSceneLoadStarted
        {
            Group = group,
            IsAdditive = true
        });

        var loadedNames = new HashSet<string>();
        var scenes = group.Scenes;

        // Build all async operations
        var ops = new List<AsyncOperation>(scenes.Length);
        for (int i = 0; i < scenes.Length; i++)
        {
            SceneReference s = scenes[i];
            if (s == null || !s.IsValid || _loadedScenes.Contains(s.SceneName)) continue;

            AsyncOperation op = SceneManager.LoadSceneAsync(s.SceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[SceneLoader] Could not start load for '{s.SceneName}' (check Build Settings).");
                continue;
            }
            op.allowSceneActivation = false;
            ops.Add(op);
            loadedNames.Add(s.SceneName);
        }

        // Wait until all are ready (progress 0.9 = ready to activate)
        bool allReady = false;
        while (!allReady)
        {
            allReady = true;
            float total = 0f;
            foreach (var op in ops)
            {
                total += op.progress;
                if (op.progress < 0.9f) allReady = false;
            }

            float normProgress = ops.Count > 0 ? total / (ops.Count * 0.9f) : 1f;
            EventBus<SceneLoadingEvents.OnSceneLoadProgress>.Raise(
                new SceneLoadingEvents.OnSceneLoadProgress { Progress = normProgress * 0.9f });

            yield return Timing.WaitForOneFrame;
        }

        // Enforce minimum loading screen time
        float elapsed = Time.unscaledTime - loadStart;
        if (elapsed < minimumLoadingScreenDuration)
            yield return Timing.WaitForSeconds(minimumLoadingScreenDuration - elapsed);

        // Activate all scenes
        foreach (var op in ops)
            op.allowSceneActivation = true;

        // Wait for activation
        bool allDone = false;
        while (!allDone)
        {
            allDone = true;
            foreach (var op in ops)
                if (!op.isDone) allDone = false;
            yield return Timing.WaitForOneFrame;
        }

        // Register loaded scenes
        foreach (string n in loadedNames)
            _loadedScenes.Add(n);

        _loadedGroups[group] = loadedNames;

        // Set active scene
        SceneReference activeRef = group.ActiveScene;
        if (activeRef != null && activeRef.IsValid)
        {
            Scene activeScene = SceneManager.GetSceneByName(activeRef.SceneName);
            if (activeScene.IsValid())
                SceneManager.SetActiveScene(activeScene);
        }

        // Reconcile skybox now that the active scene is established — the active scene's
        // SceneSkyboxController (if any) will already have registered itself on OnEnable.
        if (SkyboxManager.HasInstance)
            SkyboxManager.Instance.Reconcile();

        EventBus<SceneLoadingEvents.OnSceneLoadProgress>.Raise(
            new SceneLoadingEvents.OnSceneLoadProgress { Progress = 1f });

        EventBus<SceneLoadingEvents.OnSceneLoadCompleted>.Raise(new SceneLoadingEvents.OnSceneLoadCompleted
        {
            Group = group
        });

        // Hide loading screen
        if (showLoadingScreen)
            yield return Timing.WaitUntilDone(
                Timing.RunCoroutine(HideLoadingScreen().CancelWith(this), Segment.Update));

        _isOperationInProgress = false;
    }

    private IEnumerator<float> UnloadGroupRoutine(SceneGroup group)
    {
        if (_isOperationInProgress)
            yield return Timing.WaitUntilDone(
                Timing.RunCoroutine(WaitForOperationFree().CancelWith(this), Segment.Update));

        _isOperationInProgress = true;

        EventBus<SceneLoadingEvents.OnSceneUnloadStarted>.Raise(new SceneLoadingEvents.OnSceneUnloadStarted
        {
            Group = group
        });

        if (!_loadedGroups.TryGetValue(group, out HashSet<string> groupScenes))
        {
            _isOperationInProgress = false;
            yield break;
        }

        var ops = new List<AsyncOperation>(groupScenes.Count);
        foreach (string sceneName in groupScenes)
        {
            // Never unload the main scene
            if (mainScene != null && sceneName == mainScene.SceneName) continue;

            AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);
            if (op != null) ops.Add(op);
        }

        while (true)
        {
            bool allDone = true;
            foreach (var op in ops)
                if (!op.isDone) allDone = false;
            if (allDone) break;
            yield return Timing.WaitForOneFrame;
        }

        foreach (string sceneName in groupScenes)
            _loadedScenes.Remove(sceneName);

        _loadedGroups.Remove(group);

        // Reconcile skybox: the unloaded scenes' controllers have self-deregistered,
        // so pick the next best remaining skybox.
        if (SkyboxManager.HasInstance)
            SkyboxManager.Instance.Reconcile();

        EventBus<SceneLoadingEvents.OnSceneUnloadCompleted>.Raise(new SceneLoadingEvents.OnSceneUnloadCompleted
        {
            Group = group
        });

        _isOperationInProgress = false;
    }

    private IEnumerator<float> TransitionGroupRoutine(SceneGroup next, SceneGroup previous)
    {
        // Wait for any in-progress ops
        if (_isOperationInProgress)
            yield return Timing.WaitUntilDone(
                Timing.RunCoroutine(WaitForOperationFree().CancelWith(this), Segment.Update));

        // Show loading screen first
        yield return Timing.WaitUntilDone(
            Timing.RunCoroutine(ShowLoadingScreen().CancelWith(this), Segment.Update));

        float loadStart = Time.unscaledTime;

        // Unload previous
        if (previous != null && IsGroupLoaded(previous))
        {
            yield return Timing.WaitUntilDone(
                Timing.RunCoroutine(UnloadGroupRoutine(previous).CancelWith(this), Segment.Update));
        }

        // Load next (without its own loading screen — we own it)
        yield return Timing.WaitUntilDone(
            Timing.RunCoroutine(LoadGroupRoutine(next, showLoadingScreen: false).CancelWith(this), Segment.Update));

        // Enforce min duration
        float elapsed = Time.unscaledTime - loadStart;
        if (elapsed < minimumLoadingScreenDuration)
            yield return Timing.WaitForSeconds(minimumLoadingScreenDuration - elapsed);

        yield return Timing.WaitUntilDone(
            Timing.RunCoroutine(HideLoadingScreen().CancelWith(this), Segment.Update));
    }


    
    private IEnumerator<float> ShowLoadingScreen()
    {
        if (loadingScreenScene == null || !loadingScreenScene.IsValid) yield break;
        if (_loadedScenes.Contains(loadingScreenScene.SceneName)) yield break;

        AsyncOperation op = SceneManager.LoadSceneAsync(loadingScreenScene.SceneName, LoadSceneMode.Additive);
        if (op == null) yield break;
        while (!op.isDone) yield return Timing.WaitForOneFrame;
    }

    private IEnumerator<float> HideLoadingScreen()
    {
        if (loadingScreenScene == null || !loadingScreenScene.IsValid) yield break;
        if (!_loadedScenes.Contains(loadingScreenScene.SceneName)) yield break;

        AsyncOperation op = SceneManager.UnloadSceneAsync(loadingScreenScene.SceneName);
        if (op == null) yield break;
        while (!op.isDone) yield return Timing.WaitForOneFrame;
        _loadedScenes.Remove(loadingScreenScene.SceneName);
    }


    private IEnumerator<float> WaitForOperationFree()
    {
        while (_isOperationInProgress)
            yield return Timing.WaitForOneFrame;
    }

    private static bool ValidateReference(SceneReference scene)
    {
        if (scene == null)
        {
            Debug.LogError("[SceneLoader] SceneReference is null.");
            return false;
        }
        if (!scene.IsValid)
        {
            Debug.LogError($"[SceneLoader] SceneReference '{scene.name}' has no scene assigned.");
            return false;
        }
        return true;
    }

    private static bool ValidateGroup(SceneGroup group)
    {
        if (group == null)
        {
            Debug.LogError("[SceneLoader] SceneGroup is null.");
            return false;
        }
        if (!group.IsValid)
        {
            Debug.LogError($"[SceneLoader] SceneGroup '{group.name}' contains no valid scenes.");
            return false;
        }
        return true;
    }

    #endregion

}


