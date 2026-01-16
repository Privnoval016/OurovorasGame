using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ProceduralGrammarGeneration.GrammarParsing;

namespace ProceduralGrammarGeneration.Editor
{
    /// <summary>
    /// Professional visual editor window for creating and editing grammar assets.
    /// Provides a node-graph-like interface for defining symbols and production rules.
    /// </summary>
    public class GrammarEditorWindow : EditorWindow
    {
        private GrammarAsset targetAsset;
        private Vector2 scrollPosition;
        
        // Visual state
        private int selectedTab = 0;
        private string[] tabNames = { "Symbols", "Rules", "Settings", "Export" };
        
        // Symbol editing
        private int selectedSymbolIndex = -1;
        private Vector2 symbolScrollPos;
        
        // Rule editing
        private int selectedRuleIndex = -1;
        private int selectedProductionIndex = -1;
        private Vector2 ruleScrollPos;
        
        // Styles
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;
        private GUIStyle buttonStyle;
        private bool stylesInitialized = false;
        
        [MenuItem("Window/Procedural Grammar/Grammar Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<GrammarEditorWindow>("Grammar Editor");
            window.minSize = new Vector2(800, 600);
        }
        
        public static void OpenAsset(GrammarAsset asset)
        {
            var window = GetWindow<GrammarEditorWindow>("Grammar Editor");
            window.targetAsset = asset;
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            stylesInitialized = false;
        }
        
        private void InitializeStyles()
        {
            if (stylesInitialized) return;
            
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 5)
            };
            
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };
            
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold
            };
            
            stylesInitialized = true;
        }
        
        private void OnGUI()
        {
            InitializeStyles();
            
            // Handle object picker result
            if (Event.current.commandName == "ObjectSelectorClosed")
            {
                var selectedAsset = EditorGUIUtility.GetObjectPickerObject() as GrammarAsset;
                if (selectedAsset != null)
                {
                    targetAsset = selectedAsset;
                    Repaint();
                }
            }
            
            // Header
            DrawHeader();
            
            if (targetAsset == null)
            {
                DrawNoAssetSelected();
                return;
            }
            
            // Tabs
            EditorGUILayout.Space(5);
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));
            EditorGUILayout.Space(10);
            
            // Content based on selected tab
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            switch (selectedTab)
            {
                case 0: DrawSymbolsTab(); break;
                case 1: DrawRulesTab(); break;
                case 2: DrawSettingsTab(); break;
                case 3: DrawExportTab(); break;
            }
            
            EditorGUILayout.EndScrollView();
            
            // Footer with validation
            DrawFooter();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label("Procedural Grammar Editor", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            EditorGUI.BeginChangeCheck();
            targetAsset = (GrammarAsset)EditorGUILayout.ObjectField(targetAsset, typeof(GrammarAsset), false, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck() && targetAsset != null)
            {
                selectedSymbolIndex = -1;
                selectedRuleIndex = -1;
            }
            
            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                CreateNewGrammarAsset();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawNoAssetSelected()
        {
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginVertical(boxStyle, GUILayout.Width(400));
            GUILayout.Label("No Grammar Asset Selected", headerStyle);
            GUILayout.Space(10);
            GUILayout.Label("Select a Grammar Asset from the project or create a new one.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("Create New Grammar Asset", buttonStyle, GUILayout.Height(35)))
            {
                CreateNewGrammarAsset();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Select Existing Asset", GUILayout.Height(30)))
            {
                // Use object picker to select grammar asset
                EditorGUIUtility.ShowObjectPicker<GrammarAsset>(null, false, "", 0);
            }
            
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
        }
        
        private void DrawSymbolsTab()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Left panel - Symbol list
            EditorGUILayout.BeginVertical(boxStyle, GUILayout.Width(250));
            GUILayout.Label("Symbols", headerStyle);
            
            symbolScrollPos = EditorGUILayout.BeginScrollView(symbolScrollPos, GUILayout.ExpandHeight(true));
            
            for (int i = 0; i < targetAsset.symbols.Count; i++)
            {
                var symbol = targetAsset.symbols[i];
                var isSelected = selectedSymbolIndex == i;
                
                var bgColor = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : Color.clear;
                GUI.backgroundColor = bgColor;
                
                EditorGUILayout.BeginHorizontal(boxStyle);
                GUI.backgroundColor = Color.white;
                
                if (GUILayout.Button(symbol.name, EditorStyles.label))
                {
                    selectedSymbolIndex = i;
                }
                
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Delete Symbol", 
                        $"Are you sure you want to delete symbol '{symbol.name}'?", "Delete", "Cancel"))
                    {
                        targetAsset.symbols.RemoveAt(i);
                        if (selectedSymbolIndex == i) selectedSymbolIndex = -1;
                        EditorUtility.SetDirty(targetAsset);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("+ Add Symbol", buttonStyle, GUILayout.Height(30)))
            {
                targetAsset.AddSymbol();
                selectedSymbolIndex = targetAsset.symbols.Count - 1;
                EditorUtility.SetDirty(targetAsset);
            }
            
            EditorGUILayout.EndVertical();
            
            // Right panel - Symbol details
            EditorGUILayout.BeginVertical(boxStyle);
            
            if (selectedSymbolIndex >= 0 && selectedSymbolIndex < targetAsset.symbols.Count)
            {
                DrawSymbolDetails(targetAsset.symbols[selectedSymbolIndex]);
            }
            else
            {
                GUILayout.Label("Select a symbol to edit", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSymbolDetails(SerializedSymbol symbol)
        {
            GUILayout.Label("Symbol Details", headerStyle);
            
            EditorGUI.BeginChangeCheck();
            
            symbol.name = EditorGUILayout.TextField("Name", symbol.name);
            
            EditorGUILayout.Space(10);
            GUILayout.Label("Parameters", EditorStyles.boldLabel);
            
            // Parameters list
            for (int i = 0; i < symbol.parameters.Count; i++)
            {
                EditorGUILayout.BeginVertical(boxStyle);
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Parameter {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    symbol.parameters.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
                
                var param = symbol.parameters[i];
                param.name = EditorGUILayout.TextField("Name", param.name);
                param.type = (ParameterType)EditorGUILayout.EnumPopup("Type", param.type);
                
                // Show spatial reference for spatial types
                if (IsSpatialType(param.type))
                {
                    param.spatialDataReference = EditorGUILayout.ObjectField(
                        "Spatial Data", param.spatialDataReference, typeof(UnityEngine.Object), true);
                }
                else
                {
                    param.defaultValue = EditorGUILayout.TextField("Default Value", param.defaultValue);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            if (GUILayout.Button("+ Add Parameter", GUILayout.Height(25)))
            {
                symbol.parameters.Add(new SerializedParameter());
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(targetAsset);
            }
        }
        
        private void DrawRulesTab()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Left panel - Rules list
            EditorGUILayout.BeginVertical(boxStyle, GUILayout.Width(250));
            GUILayout.Label("Rules", headerStyle);
            
            ruleScrollPos = EditorGUILayout.BeginScrollView(ruleScrollPos, GUILayout.ExpandHeight(true));
            
            for (int i = 0; i < targetAsset.rules.Count; i++)
            {
                var rule = targetAsset.rules[i];
                var isSelected = selectedRuleIndex == i;
                
                var bgColor = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : Color.clear;
                GUI.backgroundColor = bgColor;
                
                EditorGUILayout.BeginHorizontal(boxStyle);
                GUI.backgroundColor = Color.white;
                
                if (GUILayout.Button(rule.name, EditorStyles.label))
                {
                    selectedRuleIndex = i;
                    selectedProductionIndex = 0;
                }
                
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Delete Rule", 
                        $"Are you sure you want to delete rule '{rule.name}'?", "Delete", "Cancel"))
                    {
                        targetAsset.rules.RemoveAt(i);
                        if (selectedRuleIndex == i) selectedRuleIndex = -1;
                        EditorUtility.SetDirty(targetAsset);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("+ Add Rule", buttonStyle, GUILayout.Height(30)))
            {
                targetAsset.AddRule();
                selectedRuleIndex = targetAsset.rules.Count - 1;
                EditorUtility.SetDirty(targetAsset);
            }
            
            EditorGUILayout.EndVertical();
            
            // Right panel - Rule details
            EditorGUILayout.BeginVertical(boxStyle);
            
            if (selectedRuleIndex >= 0 && selectedRuleIndex < targetAsset.rules.Count)
            {
                DrawRuleDetails(targetAsset.rules[selectedRuleIndex]);
            }
            else
            {
                GUILayout.Label("Select a rule to edit", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawRuleDetails(SerializedRule rule)
        {
            GUILayout.Label("Rule Details", headerStyle);
            
            EditorGUI.BeginChangeCheck();
            
            rule.name = EditorGUILayout.TextField("Rule Name", rule.name);
            
            EditorGUILayout.Space(10);
            
            // Predecessor
            EditorGUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Predecessor (Left Side)", EditorStyles.boldLabel);
            
            if (targetAsset.symbols.Count > 0)
            {
                var symbolNames = new List<string>();
                int currentIndex = 0;
                
                for (int i = 0; i < targetAsset.symbols.Count; i++)
                {
                    symbolNames.Add(targetAsset.symbols[i].name);
                    if (targetAsset.symbols[i].name == rule.predecessor.name)
                        currentIndex = i;
                }
                
                int newIndex = EditorGUILayout.Popup("Symbol", currentIndex, symbolNames.ToArray());
                if (newIndex != currentIndex)
                {
                    rule.predecessor = new SerializedSymbol(targetAsset.symbols[newIndex].name);
                    // Copy parameters from symbol definition
                    foreach (var param in targetAsset.symbols[newIndex].parameters)
                    {
                        rule.predecessor.parameters.Add(new SerializedParameter(param.name, param.type, param.defaultValue));
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No symbols defined. Create symbols first.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Productions
            GUILayout.Label("Productions (Right Side)", EditorStyles.boldLabel);
            
            for (int p = 0; p < rule.productions.Count; p++)
            {
                DrawProductionDetails(rule, rule.productions[p], p);
            }
            
            if (GUILayout.Button("+ Add Production", GUILayout.Height(25)))
            {
                rule.productions.Add(new SerializedProduction());
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(targetAsset);
            }
        }
        
        private void DrawProductionDetails(SerializedRule rule, SerializedProduction production, int index)
        {
            var isSelected = selectedProductionIndex == index;
            var bgColor = isSelected ? new Color(0.9f, 0.9f, 0.6f, 0.2f) : new Color(0.8f, 0.8f, 0.8f, 0.1f);
            GUI.backgroundColor = bgColor;
            
            EditorGUILayout.BeginVertical(boxStyle);
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button($"Production {index + 1}", EditorStyles.boldLabel))
            {
                selectedProductionIndex = index;
            }
            
            GUILayout.FlexibleSpace();
            
            if (rule.productions.Count > 1)
            {
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    rule.productions.RemoveAt(index);
                    return;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (rule.productions.Count > 1)
            {
                production.weight = EditorGUILayout.FloatField("Weight", production.weight);
            }
            
            // Conditions
            if (production.conditions.Count > 0 || GUILayout.Button("+ Add Condition", GUILayout.Height(20)))
            {
                if (production.conditions.Count == 0)
                    production.conditions.Add(new SerializedCondition());
                    
                for (int c = 0; c < production.conditions.Count; c++)
                {
                    EditorGUILayout.BeginHorizontal();
                    var cond = production.conditions[c];
                    cond.leftOperand = EditorGUILayout.TextField(cond.leftOperand, GUILayout.Width(80));
                    cond.op = (ConditionOperator)EditorGUILayout.EnumPopup(cond.op, GUILayout.Width(60));
                    cond.rightOperand = EditorGUILayout.TextField(cond.rightOperand, GUILayout.Width(80));
                    if (GUILayout.Button("×", GUILayout.Width(20)))
                    {
                        production.conditions.RemoveAt(c);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.Space(5);
            GUILayout.Label("Steps:", EditorStyles.boldLabel);
            
            // Production steps
            for (int s = 0; s < production.steps.Count; s++)
            {
                DrawProductionStep(production.steps[s], s, production);
            }
            
            if (GUILayout.Button("+ Add Step", GUILayout.Height(20)))
            {
                production.steps.Add(new SerializedProductionStep());
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawProductionStep(SerializedProductionStep step, int index, SerializedProduction production)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Step {index + 1}", GUILayout.Width(50));
            step.type = (ProductionStepType)EditorGUILayout.EnumPopup(step.type);
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                production.steps.RemoveAt(index);
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            switch (step.type)
            {
                case ProductionStepType.Symbol:
                    // Symbol selection
                    if (targetAsset.symbols.Count > 0)
                    {
                        var symbolNames = new List<string> { "(none)" };
                        int currentIndex = 0;
                        
                        for (int i = 0; i < targetAsset.symbols.Count; i++)
                        {
                            symbolNames.Add(targetAsset.symbols[i].name);
                            if (targetAsset.symbols[i].name == step.symbolName)
                                currentIndex = i + 1;
                        }
                        
                        int newIndex = EditorGUILayout.Popup("Symbol", currentIndex, symbolNames.ToArray());
                        if (newIndex > 0)
                        {
                            step.symbolName = targetAsset.symbols[newIndex - 1].name;
                            
                            // Show parameter assignments
                            var symbol = targetAsset.symbols[newIndex - 1];
                            
                            EditorGUI.indentLevel++;
                            foreach (var param in symbol.parameters)
                            {
                                var assignment = step.parameterAssignments.Find(a => a.parameterName == param.name);
                                if (assignment == null)
                                {
                                    assignment = new SerializedParameterAssignment(param.name, "");
                                    step.parameterAssignments.Add(assignment);
                                }
                                
                                assignment.valueExpression = EditorGUILayout.TextField(param.name, assignment.valueExpression);
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    break;
                    
                case ProductionStepType.SetParameter:
                    step.operationTarget = EditorGUILayout.TextField("Parameter", step.operationTarget);
                    step.operationValue = EditorGUILayout.TextField("Value", step.operationValue);
                    break;
                    
                case ProductionStepType.ModifyParameter:
                    step.operationTarget = EditorGUILayout.TextField("Parameter", step.operationTarget);
                    step.operationType = (OperationType)EditorGUILayout.EnumPopup("Operation", step.operationType);
                    step.operationValue = EditorGUILayout.TextField("Value", step.operationValue);
                    break;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSettingsTab()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            GUILayout.Label("Grammar Settings", headerStyle);
            
            EditorGUI.BeginChangeCheck();
            
            targetAsset.grammarName = EditorGUILayout.TextField("Grammar Name", targetAsset.grammarName);
            targetAsset.axiom = EditorGUILayout.TextField("Axiom (Start Symbol)", targetAsset.axiom);
            targetAsset.maxIterations = EditorGUILayout.IntField("Max Iterations", targetAsset.maxIterations);
            
            EditorGUILayout.Space(20);
            
            GUILayout.Label("File Settings", EditorStyles.boldLabel);
            targetAsset.filePath = EditorGUILayout.TextField("Export Path", targetAsset.filePath);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                var path = EditorUtility.SaveFilePanel("Select Export Path", "Assets", targetAsset.grammarName, "pgr");
                if (!string.IsNullOrEmpty(path))
                {
                    targetAsset.filePath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(targetAsset);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawExportTab()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            GUILayout.Label("Export & Import", headerStyle);
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Export to .PGR File", buttonStyle, GUILayout.Height(40)))
            {
                ExportToPGR();
            }
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Import from .PGR File", buttonStyle, GUILayout.Height(40)))
            {
                ImportFromPGR();
            }
            
            EditorGUILayout.Space(20);
            
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            var pgrContent = GrammarAssetIO.ExportToPGR(targetAsset);
            EditorGUILayout.TextArea(pgrContent, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Validation
            var errors = targetAsset.Validate();
            if (errors.Count > 0)
            {
                GUI.color = new Color(1f, 0.5f, 0.5f);
                GUILayout.Label($"⚠ {errors.Count} validation error(s)", EditorStyles.boldLabel);
                GUI.color = Color.white;
                
                if (GUILayout.Button("Show Errors", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    var message = string.Join("\n", errors);
                    EditorUtility.DisplayDialog("Validation Errors", message, "OK");
                }
            }
            else
            {
                GUI.color = new Color(0.5f, 1f, 0.5f);
                GUILayout.Label("✓ Grammar is valid", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            
            GUILayout.FlexibleSpace();
            
            GUILayout.Label($"Symbols: {targetAsset.symbols.Count} | Rules: {targetAsset.rules.Count}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void CreateNewGrammarAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create New Grammar Asset",
                "NewGrammar",
                "asset",
                "Enter a name for the new grammar asset");
                
            if (!string.IsNullOrEmpty(path))
            {
                var asset = CreateInstance<GrammarAsset>();
                asset.grammarName = System.IO.Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                targetAsset = asset;
                Selection.activeObject = asset;
            }
        }
        
        private void ExportToPGR()
        {
            var path = targetAsset.filePath;
            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFilePanel("Export Grammar", "Assets", targetAsset.grammarName, "pgr");
            }
            
            if (!string.IsNullOrEmpty(path))
            {
                var content = GrammarAssetIO.ExportToPGR(targetAsset);
                System.IO.File.WriteAllText(path, content);
                targetAsset.filePath = path;
                EditorUtility.SetDirty(targetAsset);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Export Successful", $"Grammar exported to:\n{path}", "OK");
            }
        }
        
        private void ImportFromPGR()
        {
            var path = EditorUtility.OpenFilePanel("Import Grammar", "Assets", "pgr");
            if (!string.IsNullOrEmpty(path))
            {
                var content = System.IO.File.ReadAllText(path);
                GrammarAssetIO.ImportFromPGR(targetAsset, content);
                targetAsset.filePath = path;
                EditorUtility.SetDirty(targetAsset);
                EditorUtility.DisplayDialog("Import Successful", "Grammar imported successfully!", "OK");
            }
        }
        
        private bool IsSpatialType(ParameterType type)
        {
            return type == ParameterType.SplineCurve || 
                   type == ParameterType.Heightmap || 
                   type == ParameterType.PointField ||
                   type == ParameterType.SpatialConstant;
        }
    }
}
