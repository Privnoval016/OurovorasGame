using UnityEngine;
using UnityEditor;
using System;

namespace Quests.Editor
{
    public class CreateQuestAssetDialog : EditorWindow
    {
        private string assetName = "NewQuest";
        private string savePath = "Assets/Scripts/Quests/Quests";
        private bool isOutcome;
        private Action<string, string> onCreateCallback;
        
        public static void ShowDialog(bool isOutcome, Action<string, string> onCreate)
        {
            CreateQuestAssetDialog window = GetWindow<CreateQuestAssetDialog>(true, 
                isOutcome ? "Create Quest Outcome" : "Create Quest Objective", true);
            window.minSize = new Vector2(400, 150);
            window.maxSize = new Vector2(400, 150);
            window.isOutcome = isOutcome;
            window.onCreateCallback = onCreate;
            window.ShowUtility();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField(isOutcome ? "Create New Quest Outcome" : "Create New Quest Objective", 
                EditorStyles.boldLabel);
            
            EditorGUILayout.Space(10);
            
            // Asset Name
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Asset Name:", GUILayout.Width(100));
            assetName = EditorGUILayout.TextField(assetName);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Save Path
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Save Path:", GUILayout.Width(100));
            EditorGUILayout.TextField(savePath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Save Location", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Convert absolute path to relative
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        savePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Validation
            bool isValid = !string.IsNullOrEmpty(assetName) && 
                          !string.IsNullOrEmpty(savePath) && 
                          AssetDatabase.IsValidFolder(savePath);
            
            if (!isValid)
            {
                EditorGUILayout.HelpBox("Please enter a valid name and select an existing folder.", MessageType.Warning);
            }
            
            EditorGUILayout.Space(10);
            
            // Buttons
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Cancel", GUILayout.Width(100), GUILayout.Height(30)))
            {
                Close();
            }
            
            GUI.enabled = isValid;
            if (GUILayout.Button("Create", GUILayout.Width(100), GUILayout.Height(30)))
            {
                onCreateCallback?.Invoke(assetName, savePath);
                Close();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
        }
    }
}

