#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Extensions.Dialogue.Editor
{
    /// <summary>
    /// Menu items for creating dialogue assets and opening the editor.
    /// </summary>
    public static class DialogueEditorMenuItems
    {
        [MenuItem("Assets/Create/Dialogue/Dialogue Graph")]
        public static void CreateDialogueGraph()
        {
            var graph = ScriptableObject.CreateInstance<Data.DialogueGraph>();
            string path = EditorUtility.SaveFilePanelInProject("Create Dialogue Graph", "NewDialogue", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                graph.GraphName = "New Dialogue";
                AssetDatabase.CreateAsset(graph, path);
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("Assets/Create/Dialogue/Asset Manager")]
        public static void CreateAssetManager()
        {
            var manager = ScriptableObject.CreateInstance<Integration.DialogueAssetManager>();
            string path = EditorUtility.SaveFilePanelInProject("Create Dialogue Asset Manager", "DialogueAssetManager", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(manager, path);
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("Tools/Dialogue System/Open Graph Editor")]
        public static void OpenGraphEditor()
        {
            DialogueGraphWindow.ShowWindow();
        }
    }
}
#endif

