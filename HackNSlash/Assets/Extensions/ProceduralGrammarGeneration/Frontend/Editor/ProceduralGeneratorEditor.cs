using UnityEngine;
using UnityEditor;
using ProceduralGrammarGeneration.Spatial;
using ProceduralGrammarGeneration.Runtime;
using System.Collections.Generic;
using ProceduralGrammarGeneration.GrammarParsing;

namespace ProceduralGrammarGeneration.Frontend.Editor
{
    /// <summary>
    /// Custom editor for ProceduralGenerator with polished, intuitive UI
    /// </summary>
    [CustomEditor(typeof(ProceduralGenerator))]
    public class ProceduralGeneratorEditor : UnityEditor.Editor
    {
        private ProceduralGenerator _generator;
        private SerializedProperty _grammarAssetProp;
        private SerializedProperty _maxIterationsProp;
        private SerializedProperty _spatialStrategyProp;
        private SerializedProperty _geometryLibraryProp;
        private SerializedProperty _axiomParametersProp;
        private SerializedProperty _includeNonTerminalsProp;
        private SerializedProperty _autoClearPreviousProp;
        private SerializedProperty _generationStatusProp;
        private SerializedProperty _lastStatsProp;
        
        // Foldout states
        private bool _grammarFoldout = true;
        private bool _strategyFoldout = true;
        private bool _geometryFoldout = true;
        private bool _parametersFoldout = true;
        private bool _optionsFoldout = false;
        private bool _statsFoldout = false;
        
        // Strategy type selection
        private int _strategyTypeIndex = 0;
        private readonly string[] _strategyTypeNames = { "Vertical Stack", "Spline Follow" };
        
        // Style cache
        private GUIStyle _headerStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _bigButtonStyle;
        private bool _stylesInitialized = false;
        
        private void OnEnable()
        {
            _generator = (ProceduralGenerator)target;
            
            _grammarAssetProp = serializedObject.FindProperty("grammarAsset");
            _maxIterationsProp = serializedObject.FindProperty("maxIterations");
            _spatialStrategyProp = serializedObject.FindProperty("spatialStrategy");
            _geometryLibraryProp = serializedObject.FindProperty("geometryLibrary");
            _axiomParametersProp = serializedObject.FindProperty("axiomParameters");
            _includeNonTerminalsProp = serializedObject.FindProperty("includeNonTerminals");
            _autoClearPreviousProp = serializedObject.FindProperty("autoClearPrevious");
            _generationStatusProp = serializedObject.FindProperty("_generationStatus");
            _lastStatsProp = serializedObject.FindProperty("_lastStats");
        }
        
        public override void OnInspectorGUI()
        {
            InitializeStyles();
            serializedObject.Update();
            
            // Header
            DrawHeader();
            
            EditorGUILayout.Space(10);
            
            // Grammar Configuration Section
            DrawGrammarSection();
            
            EditorGUILayout.Space(5);
            
            // Spatial Strategy Section
            DrawStrategySection();
            
            EditorGUILayout.Space(5);
            
            // Geometry Library Section
            DrawGeometrySection();
            
            EditorGUILayout.Space(5);
            
            // Parameters Section
            DrawParametersSection();
            
            EditorGUILayout.Space(5);
            
            // Options Section
            DrawOptionsSection();
            
            EditorGUILayout.Space(10);
            
            // Generation Controls
            DrawGenerationControls();
            
            EditorGUILayout.Space(5);
            
            // Status Section
            DrawStatusSection();
            
            EditorGUILayout.Space(5);
            
            // Statistics Section
            DrawStatisticsSection();
            
            serializedObject.ApplyModifiedProperties();
            
            // Auto-update parameters when grammar changes
            if (GUI.changed)
            {
                _generator.UpdateAxiomParameters();
            }
        }
        
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.9f, 1f) }
            };
            
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11
            };
            
            _bigButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                fixedHeight = 40
            };
            
            _stylesInitialized = true;
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Procedural Grammar Generator", _headerStyle);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "Generate procedural 3D structures from grammar rules. " +
                "Configure your grammar, spatial strategy, and geometry assets, then click Generate.",
                MessageType.Info
            );
        }
        
        private void DrawGrammarSection()
        {
            _grammarFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_grammarFoldout, "Grammar Configuration");
            
            if (_grammarFoldout)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(_grammarAssetProp, new GUIContent("Grammar Asset", "The grammar asset defining generation rules"));
                
                if (_generator.grammarAsset == null)
                {
                    EditorGUILayout.HelpBox("Assign a Grammar Asset to begin. Create one via: Assets > Create > Procedural Grammar > Grammar Asset", MessageType.Warning);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField("Axiom Symbol", _generator.grammarAsset.axiom);
                    EditorGUILayout.IntField("Symbols", _generator.grammarAsset.symbols.Count);
                    EditorGUILayout.IntField("Rules", _generator.grammarAsset.rules.Count);
                    EditorGUI.EndDisabledGroup();
                }
                
                EditorGUILayout.PropertyField(_maxIterationsProp, new GUIContent("Max Iterations", "Maximum number of grammar rule expansions"));
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private void DrawStrategySection()
        {
            _strategyFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_strategyFoldout, "Spatial Strategy");
            
            if (_strategyFoldout)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.HelpBox(
                    "The spatial strategy determines how grammar symbols are positioned in 3D space.",
                    MessageType.None
                );
                
                // Strategy type selector
                EditorGUI.BeginChangeCheck();
                _strategyTypeIndex = EditorGUILayout.Popup("Strategy Type", _strategyTypeIndex, _strategyTypeNames);
                if (EditorGUI.EndChangeCheck())
                {
                    CreateStrategyOfType(_strategyTypeIndex);
                }
                
                // Initialize strategy if null
                if (_generator.spatialStrategy == null)
                {
                    CreateStrategyOfType(0);
                }
                
                // Draw strategy properties
                if (_generator.spatialStrategy != null)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Strategy Settings", EditorStyles.boldLabel);
                    
                    // Draw strategy properties without nested foldout
                    EditorGUI.indentLevel++;
                    
                    // Iterate through child properties manually to avoid nested foldouts
                    SerializedProperty iterator = _spatialStrategyProp.Copy();
                    SerializedProperty endProperty = iterator.GetEndProperty();
                    
                    iterator.NextVisible(true); // Enter first child
                    while (!SerializedProperty.EqualContents(iterator, endProperty))
                    {
                        EditorGUILayout.PropertyField(iterator, false);
                        if (!iterator.NextVisible(false)) break;
                    }
                    
                    EditorGUI.indentLevel--;
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private void DrawGeometrySection()
        {
            _geometryFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_geometryFoldout, "Geometry Library");
            
            if (_geometryFoldout)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.HelpBox(
                    "Assign geometry data for terminal symbols (Window, Door, Wall, etc.). Each geometry asset defines the visual representation.",
                    MessageType.None
                );
                
                EditorGUILayout.PropertyField(_geometryLibraryProp, new GUIContent("Geometry Assets"), true);
                
                if (_geometryLibraryProp.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("Add geometry assets for terminal symbols. Without them, cube placeholders will be used.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField($"Total Assets: {_geometryLibraryProp.arraySize}", EditorStyles.miniLabel);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private void DrawParametersSection()
        {
            _parametersFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_parametersFoldout, "Axiom Parameters");
            
            if (_parametersFoldout)
            {
                EditorGUI.indentLevel++;
                
                if (_generator.grammarAsset == null)
                {
                    EditorGUILayout.HelpBox("Assign a Grammar Asset to configure parameters.", MessageType.Info);
                }
                else if (_axiomParametersProp.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("No parameters required for this axiom.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Configure initial values for axiom parameters. These values are passed to the grammar's starting symbol.",
                        MessageType.None
                    );
                    
                    EditorGUILayout.Space(5);
                    
                    // Get strategy-provided parameter names
                    var strategyProvidedParams = GetStrategyProvidedParameters();
                    
                    // Draw parameters with better formatting
                    for (int i = 0; i < _axiomParametersProp.arraySize; i++)
                    {
                        SerializedProperty paramProp = _axiomParametersProp.GetArrayElementAtIndex(i);
                        SerializedProperty nameProp = paramProp.FindPropertyRelative("name");
                        SerializedProperty typeProp = paramProp.FindPropertyRelative("type");
                        
                        string paramName = nameProp.stringValue;
                        bool isStrategyProvided = strategyProvidedParams.Contains(paramName);
                        
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        
                        // Show info if parameter comes from strategy
                        if (isStrategyProvided)
                        {
                            EditorGUILayout.LabelField(paramName, EditorStyles.boldLabel);
                            EditorGUILayout.LabelField("(Auto-populated from strategy)", EditorStyles.miniLabel);
                            GUI.enabled = false; // Make read-only
                        }
                        
                        ParameterType paramType = (ParameterType)typeProp.enumValueIndex;
                        
                        switch (paramType)
                        {
                            case ParameterType.Float:
                                SerializedProperty floatProp = paramProp.FindPropertyRelative("floatValue");
                                EditorGUILayout.PropertyField(floatProp, new GUIContent(isStrategyProvided ? "Value (from strategy)" : paramName));
                                break;
                            case ParameterType.Int:
                                SerializedProperty intProp = paramProp.FindPropertyRelative("intValue");
                                EditorGUILayout.PropertyField(intProp, new GUIContent(isStrategyProvided ? "Value (from strategy)" : paramName));
                                break;
                            case ParameterType.String:
                                SerializedProperty stringProp = paramProp.FindPropertyRelative("stringValue");
                                EditorGUILayout.PropertyField(stringProp, new GUIContent(isStrategyProvided ? "Value (from strategy)" : paramName));
                                break;
                            case ParameterType.Bool:
                                SerializedProperty boolProp = paramProp.FindPropertyRelative("boolValue");
                                EditorGUILayout.PropertyField(boolProp, new GUIContent(isStrategyProvided ? "Value (from strategy)" : paramName));
                                break;
                            case ParameterType.Vector3:
                                SerializedProperty vector3Prop = paramProp.FindPropertyRelative("vector3Value");
                                EditorGUILayout.PropertyField(vector3Prop, new GUIContent(isStrategyProvided ? "Value (from strategy)" : paramName));
                                break;
                            case ParameterType.Color:
                                SerializedProperty colorProp = paramProp.FindPropertyRelative("colorValue");
                                EditorGUILayout.PropertyField(colorProp, new GUIContent(isStrategyProvided ? "Value (from strategy)" : paramName));
                                break;
                        }
                        
                        if (isStrategyProvided)
                        {
                            GUI.enabled = true; // Re-enable
                        }
                        
                        EditorGUILayout.EndVertical();
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private void DrawOptionsSection()
        {
            _optionsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_optionsFoldout, "Advanced Options");
            
            if (_optionsFoldout)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(_includeNonTerminalsProp, new GUIContent("Include Non-Terminals", "Include non-terminal nodes in hierarchy (for debugging)"));
                EditorGUILayout.PropertyField(_autoClearPreviousProp, new GUIContent("Auto Clear Previous", "Automatically clear previous generation before creating new"));
                
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Debug Options", EditorStyles.boldLabel);
                
                SerializedProperty debugPathProp = serializedObject.FindProperty("debugOutputPath");
                EditorGUILayout.PropertyField(debugPathProp, new GUIContent("Debug Output Path", "Write grammar, derivation tree, and spatial data to this file"));
                
                if (!string.IsNullOrEmpty(_generator.debugOutputPath))
                {
                    EditorGUILayout.HelpBox("Debug output will be written to this file after each generation.", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private void DrawGenerationControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Generation Controls", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(5);
            
            // Generate button
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("GENERATE", _bigButtonStyle))
            {
                _generator.Generate();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(5);
            
            // Clear button
            EditorGUI.BeginDisabledGroup(!_generator.HasGeneration);
            GUI.backgroundColor = new Color(1f, 0.5f, 0.4f);
            if (GUILayout.Button("Clear Generated Content", _buttonStyle, GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear Generation", 
                    "Are you sure you want to clear the generated content?", 
                    "Clear", "Cancel"))
                {
                    _generator.Clear();
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStatusSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            string status = _generationStatusProp.stringValue;
            MessageType messageType = MessageType.Info;
            
            if (status.StartsWith("Error:"))
                messageType = MessageType.Error;
            else if (status.StartsWith("Complete"))
                messageType = MessageType.Info;
            else if (status == "Cleared")
                messageType = MessageType.Info;
            else if (status.Contains("..."))
                messageType = MessageType.None;
            
            EditorGUILayout.HelpBox($"Status: {status}", messageType);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStatisticsSection()
        {
            if (!_generator.HasGeneration) return;
            
            _statsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_statsFoldout, "Generation Statistics");
            
            if (_statsFoldout)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                SerializedProperty grammarNameProp = _lastStatsProp.FindPropertyRelative("grammarName");
                SerializedProperty symbolCountProp = _lastStatsProp.FindPropertyRelative("symbolCount");
                SerializedProperty ruleCountProp = _lastStatsProp.FindPropertyRelative("ruleCount");
                SerializedProperty derivationDepthProp = _lastStatsProp.FindPropertyRelative("derivationDepth");
                SerializedProperty terminalCountProp = _lastStatsProp.FindPropertyRelative("terminalCount");
                SerializedProperty spatialNodeCountProp = _lastStatsProp.FindPropertyRelative("spatialNodeCount");
                SerializedProperty generatedObjectCountProp = _lastStatsProp.FindPropertyRelative("generatedObjectCount");
                SerializedProperty boundsProp = _lastStatsProp.FindPropertyRelative("bounds");
                
                EditorGUILayout.LabelField("Grammar", grammarNameProp.stringValue, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Symbols / Rules", $"{symbolCountProp.intValue} / {ruleCountProp.intValue}");
                EditorGUILayout.LabelField("Derivation Depth", derivationDepthProp.intValue.ToString());
                EditorGUILayout.LabelField("Terminal Nodes", terminalCountProp.intValue.ToString());
                EditorGUILayout.LabelField("Spatial Nodes", spatialNodeCountProp.intValue.ToString());
                EditorGUILayout.LabelField("Generated Objects", generatedObjectCountProp.intValue.ToString());
                
                Bounds bounds = boundsProp.boundsValue;
                EditorGUILayout.LabelField("Bounds Center", bounds.center.ToString("F2"));
                EditorGUILayout.LabelField("Bounds Size", bounds.size.ToString("F2"));
                
                EditorGUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// Get list of parameter names that are automatically provided by the strategy
        /// </summary>
        private System.Collections.Generic.HashSet<string> GetStrategyProvidedParameters()
        {
            var provided = new System.Collections.Generic.HashSet<string>();
            
            if (_generator.spatialStrategy == null)
                return provided;
            
            // Initialize strategy and check what variables it provides
            var tempContext = new SpatialContext();
            _generator.spatialStrategy.Initialize(tempContext);
            
            // Common strategy-provided parameters
            // SplineFollowStrategy provides: splineLength
            // Could extend this to check tempContext.Variables for any set values
            foreach (var kvp in tempContext.Variables)
            {
                provided.Add(kvp.Key);
            }
            
            return provided;
        }
        
        private void CreateStrategyOfType(int typeIndex)
        {
            switch (typeIndex)
            {
                case 0: // Vertical Stack
                    _generator.spatialStrategy = new VerticalStackStrategy(3.5f);
                    break;
                case 1: // Spline Follow
                    _generator.spatialStrategy = new SplineFollowStrategy(null, 14f, 0f, 3.5f);
                    break;
            }
            
            EditorUtility.SetDirty(_generator);
        }
    }
}
