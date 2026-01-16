using UnityEngine;
using UnityEditor;
using ProceduralGrammarGeneration.GrammarParsing;

namespace ProceduralGrammarGeneration.Editor
{
    /// <summary>
    /// Custom inspector for GrammarAsset that provides quick access to the editor window.
    /// </summary>
    [CustomEditor(typeof(GrammarAsset))]
    public class GrammarAssetEditor : UnityEditor.Editor
    {
        private GrammarAsset grammar;
        
        private void OnEnable()
        {
            grammar = (GrammarAsset)target;
        }
        
        public override void OnInspectorGUI()
        {
            // Header
            EditorGUILayout.Space(10);
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            GUILayout.Label("Procedural Grammar Asset", headerStyle);
            
            EditorGUILayout.Space(10);
            
            // Quick info box
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("Grammar Information", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Name", grammar.grammarName);
            EditorGUILayout.LabelField("Axiom", grammar.axiom);
            EditorGUILayout.LabelField("Symbols", grammar.symbols.Count.ToString());
            EditorGUILayout.LabelField("Rules", grammar.rules.Count.ToString());
            EditorGUILayout.LabelField("Max Iterations", grammar.maxIterations.ToString());
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Validation status
            var errors = grammar.Validate();
            if (errors.Count > 0)
            {
                EditorGUILayout.HelpBox($"Grammar has {errors.Count} validation error(s). Open the editor to fix them.", MessageType.Warning);
                
                if (GUILayout.Button("Show Errors"))
                {
                    var message = string.Join("\n• ", errors);
                    EditorUtility.DisplayDialog("Validation Errors", "• " + message, "OK");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Grammar is valid and ready to use.", MessageType.Info);
            }
            
            EditorGUILayout.Space(10);
            
            // Action buttons
            if (GUILayout.Button("Open in Grammar Editor", GUILayout.Height(40)))
            {
                GrammarEditorWindow.OpenAsset(grammar);
            }
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Export to .PGR", GUILayout.Height(30)))
            {
                ExportToPGR();
            }
            
            if (GUILayout.Button("Import from .PGR", GUILayout.Height(30)))
            {
                ImportFromPGR();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Test Compile", GUILayout.Height(30)))
            {
                TestCompile();
            }
            
            EditorGUILayout.Space(20);
            
            // Advanced: Show default inspector
            if (EditorGUILayout.Foldout(SessionState.GetBool("GrammarAsset_ShowAdvanced", false), "Advanced (Raw Data)"))
            {
                SessionState.SetBool("GrammarAsset_ShowAdvanced", true);
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Editing raw data directly is not recommended. Use the Grammar Editor instead.", MessageType.Warning);
                EditorGUILayout.Space(5);
                DrawDefaultInspector();
            }
            else
            {
                SessionState.SetBool("GrammarAsset_ShowAdvanced", false);
            }
        }
        
        private void ExportToPGR()
        {
            var path = grammar.filePath;
            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFilePanel("Export Grammar", "Assets", grammar.grammarName, "pgr");
            }
            
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    var content = GrammarAssetIO.ExportToPGR(grammar);
                    System.IO.File.WriteAllText(path, content);
                    grammar.filePath = path;
                    EditorUtility.SetDirty(grammar);
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("Export Successful", $"Grammar exported to:\n{path}", "OK");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Export Failed", $"Failed to export grammar:\n{e.Message}", "OK");
                }
            }
        }
        
        private void ImportFromPGR()
        {
            var path = EditorUtility.OpenFilePanel("Import Grammar", "Assets", "pgr");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    var content = System.IO.File.ReadAllText(path);
                    GrammarAssetIO.ImportFromPGR(grammar, content);
                    grammar.filePath = path;
                    EditorUtility.SetDirty(grammar);
                    EditorUtility.DisplayDialog("Import Successful", "Grammar imported successfully!", "OK");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Import Failed", $"Failed to import grammar:\n{e.Message}", "OK");
                }
            }
        }
        
        private void TestCompile()
        {
            try
            {
                var errors = grammar.Validate();
                if (errors.Count > 0)
                {
                    EditorUtility.DisplayDialog("Compilation Failed", 
                        "Grammar has validation errors. Please fix them first.", "OK");
                    return;
                }
                
                var engine = GrammarAssetIO.CompileGrammar(grammar);
                EditorUtility.DisplayDialog("Compilation Successful", 
                    "Grammar compiled successfully! No errors detected.", "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Compilation Failed", 
                    $"Failed to compile grammar:\n{e.Message}\n\nCheck the console for details.", "OK");
                Debug.LogError($"Grammar compilation failed: {e}");
            }
        }
    }
}
