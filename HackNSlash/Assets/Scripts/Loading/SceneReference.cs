using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/**
 * <summary>
 * A ScriptableObject that holds a reference to a Unity scene asset.
 * Provides a type-safe, non-string-based way to reference scenes across the project.
 * In the Editor the full asset path is derived from the <see cref="SceneAsset"/>; in builds
 * only the scene name is available (scenes must be in Build Settings).
 * </summary>
 */
[CreateAssetMenu(menuName = "Scene Management/Scene Reference")]
public class SceneReference : ScriptableObject
{
#if UNITY_EDITOR
    /** <summary>The scene asset dragged in from the Project window. Editor-only.</summary> */
    [SerializeField] private SceneAsset sceneAsset;
#endif

    /**
     * <summary>
     * The scene name, baked at build time and used as the runtime identifier.
     * This is what Unity's SceneManager uses to identify a scene.
     * </summary>
     */
    [SerializeField, HideInInspector] private string sceneName;

#if UNITY_EDITOR
    /** <summary>Returns the full asset path of the scene (e.g. "Assets/Scenes/Main.unity"). Editor only.</summary> */
    public string ScenePath => sceneAsset != null ? AssetDatabase.GetAssetPath(sceneAsset) : string.Empty;

    private void OnValidate()
    {
        sceneName = sceneAsset != null ? sceneAsset.name : string.Empty;
    }
#endif

    /**
     * <summary>
     * The runtime scene name as registered in Build Settings.
     * Use this as the key when calling <see cref="UnityEngine.SceneManagement.SceneManager"/> methods.
     * </summary>
     */
    public string SceneName => sceneName;

    /** <summary>Returns true if this reference points to a valid scene.</summary> */
    public bool IsValid => !string.IsNullOrEmpty(sceneName);

    public override string ToString() => $"SceneReference({sceneName})";
}
