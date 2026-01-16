using UnityEngine;
using UnityEditor;
using ProceduralGrammarGeneration.GrammarParsing;

namespace ProceduralGrammarGeneration.Editor
{
    /// <summary>
    /// Provides menu items and shortcuts for quick access to grammar tools.
    /// </summary>
    public static class GrammarMenuItems
    {
        [MenuItem("Assets/Create/Procedural Grammar/Grammar Asset", false, 1)]
        public static void CreateGrammarAsset()
        {
            var asset = ScriptableObject.CreateInstance<GrammarAsset>();
            
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }
            else if (!System.IO.Directory.Exists(path))
            {
                path = System.IO.Path.GetDirectoryName(path);
            }
            
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath($"{path}/NewGrammar.asset");
            
            AssetDatabase.CreateAsset(asset, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            
            // Automatically open in editor
            GrammarEditorWindow.OpenAsset(asset);
        }
        
        [MenuItem("Assets/Create/Procedural Grammar/Composed Grammar", false, 2)]
        public static void CreateComposedGrammar()
        {
            // Get selected grammar assets
            var selectedGrammars = Selection.GetFiltered<GrammarAsset>(SelectionMode.Assets);
            
            if (selectedGrammars.Length < 2)
            {
                EditorUtility.DisplayDialog("Compose Grammars", 
                    "Please select 2 or more Grammar Assets to compose.\n\nSelect multiple assets in the Project window, then use this menu item.", 
                    "OK");
                return;
            }
            
            // Create composed grammar
            var composed = GrammarComposer.MergeGrammars("ComposedGrammar", selectedGrammars);
            
            // Save it
            string path = AssetDatabase.GetAssetPath(selectedGrammars[0]);
            path = System.IO.Path.GetDirectoryName(path);
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath($"{path}/ComposedGrammar.asset");
            
            AssetDatabase.CreateAsset(composed, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Selection.activeObject = composed;
            EditorGUIUtility.PingObject(composed);
            
            EditorUtility.DisplayDialog("Compose Grammars", 
                $"Created composed grammar with:\n• {composed.symbols.Count} symbols\n• {composed.rules.Count} rules", 
                "OK");
        }
        
        [MenuItem("Assets/Create/Procedural Grammar/Composed Grammar", true)]
        public static bool ValidateCreateComposedGrammar()
        {
            // Only enable if 2+ grammar assets are selected
            return Selection.GetFiltered<GrammarAsset>(SelectionMode.Assets).Length >= 2;
        }
        
        [MenuItem("Window/Procedural Grammar/Grammar Editor %#G")]
        public static void OpenGrammarEditor()
        {
            GrammarEditorWindow.ShowWindow();
        }
        
        [MenuItem("Window/Procedural Grammar/Documentation")]
        public static void OpenDocumentation()
        {
            var docsPath = "Assets/Extensions/ProceduralGrammarGeneration/Documentation";
            var indexPath = $"{docsPath}/INDEX.md";
            
            if (System.IO.File.Exists(indexPath))
            {
                var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(indexPath);
                if (textAsset != null)
                {
                    Selection.activeObject = textAsset;
                    EditorGUIUtility.PingObject(textAsset);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Documentation", 
                    $"Documentation not found at:\n{docsPath}\n\nPlease check the Documentation folder.", 
                    "OK");
            }
        }
        
        [MenuItem("CONTEXT/GrammarAsset/Open in Grammar Editor")]
        public static void OpenInGrammarEditor(MenuCommand command)
        {
            var asset = command.context as GrammarAsset;
            if (asset != null)
            {
                GrammarEditorWindow.OpenAsset(asset);
            }
        }
        
        [MenuItem("CONTEXT/GrammarAsset/Export to PGR File")]
        public static void ExportToPGR(MenuCommand command)
        {
            var asset = command.context as GrammarAsset;
            if (asset != null)
            {
                var path = asset.filePath;
                if (string.IsNullOrEmpty(path))
                {
                    path = EditorUtility.SaveFilePanel("Export Grammar", "Assets", asset.grammarName, "pgr");
                }
                
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        var content = GrammarAssetIO.ExportToPGR(asset);
                        System.IO.File.WriteAllText(path, content);
                        asset.filePath = path;
                        EditorUtility.SetDirty(asset);
                        AssetDatabase.Refresh();
                        Debug.Log($"Grammar exported to: {path}");
                    }
                    catch (System.Exception e)
                    {
                        EditorUtility.DisplayDialog("Export Failed", $"Failed to export:\n{e.Message}", "OK");
                    }
                }
            }
        }
        
        [MenuItem("CONTEXT/GrammarAsset/Import from PGR File")]
        public static void ImportFromPGR(MenuCommand command)
        {
            var asset = command.context as GrammarAsset;
            if (asset != null)
            {
                var path = EditorUtility.OpenFilePanel("Import Grammar", "Assets", "pgr");
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        var content = System.IO.File.ReadAllText(path);
                        GrammarAssetIO.ImportFromPGR(asset, content);
                        asset.filePath = path;
                        EditorUtility.SetDirty(asset);
                        Debug.Log($"Grammar imported from: {path}");
                    }
                    catch (System.Exception e)
                    {
                        EditorUtility.DisplayDialog("Import Failed", $"Failed to import:\n{e.Message}", "OK");
                    }
                }
            }
        }
        
        [MenuItem("Assets/Procedural Grammar/Import PGR File as Asset", false, 2)]
        public static void ImportPGRFileAsAsset()
        {
            var path = EditorUtility.OpenFilePanel("Import PGR File", "Assets", "pgr");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    var content = System.IO.File.ReadAllText(path);
                    var asset = ScriptableObject.CreateInstance<GrammarAsset>();
                    
                    GrammarAssetIO.ImportFromPGR(asset, content);
                    asset.grammarName = System.IO.Path.GetFileNameWithoutExtension(path);
                    asset.filePath = path;
                    
                    string assetPath = "Assets";
                    if (Selection.activeObject != null)
                    {
                        assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                        if (!System.IO.Directory.Exists(assetPath))
                        {
                            assetPath = System.IO.Path.GetDirectoryName(assetPath);
                        }
                    }
                    
                    string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(
                        $"{assetPath}/{asset.grammarName}.asset");
                    
                    AssetDatabase.CreateAsset(asset, assetPathAndName);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                    
                    Debug.Log($"Created Grammar Asset from: {path}");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Import Failed", 
                        $"Failed to import PGR file:\n{e.Message}", "OK");
                }
            }
        }
    }
}
