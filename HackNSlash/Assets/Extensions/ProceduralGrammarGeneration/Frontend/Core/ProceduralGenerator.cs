using System.Collections.Generic;
using System.Linq;
using ProceduralGrammarGeneration.Generation;
using UnityEngine;
using ProceduralGrammarGeneration.GrammarParsing;
using ProceduralGrammarGeneration.Runtime;
using ProceduralGrammarGeneration.Spatial;
using ProceduralGrammarGeneration.Testing;

namespace ProceduralGrammarGeneration.Frontend
{
    /// <summary>
    /// Production-ready entry point for procedural grammar generation.
    /// Provides complete pipeline from grammar definition to generated meshes.
    /// </summary>
    [ExecuteInEditMode]
    public class ProceduralGenerator : MonoBehaviour
    {
        [Header("Grammar Configuration")]
        [Tooltip("Grammar asset defining the generation rules")]
        public GrammarAsset grammarAsset;
        
        [Tooltip("Maximum iterations for grammar expansion")]
        public int maxIterations = 100;
        
        [Header("Spatial Strategy")]
        [SerializeReference]
        [Tooltip("Spatial interpretation strategy - defines how grammar symbols are positioned in 3D space")]
        public BaseSpatialStrategy spatialStrategy = new VerticalStackStrategy(3.5f);
        
        [Header("Geometry Assets")]
        [Tooltip("Visual geometry for terminal symbols (Window, Door, Wall, etc.)")]
        public SymbolGeometryData[] geometryLibrary = new SymbolGeometryData[0];
        
        [Header("Generation Parameters")]
        [Tooltip("Parameters passed to the axiom symbol (auto-populated from grammar)")]
        public List<AxiomParameter> axiomParameters = new List<AxiomParameter>();
        
        [Header("Generation Options")]
        [Tooltip("Include non-terminal nodes in generated hierarchy (for debugging)")]
        public bool includeNonTerminals = false;
        
        [Tooltip("Automatically clear previous generation before creating new one")]
        public bool autoClearPrevious = true;
        
        [Header("Debug Options")]
        [Tooltip("Optional: Write grammar and spatial data to file for debugging (leave empty to skip)")]
        public string debugOutputPath = "";
        
        [Header("Status")]
        [SerializeField]
        [Tooltip("Current generation status")]
        private string _generationStatus = "Ready";
        
        [SerializeField]
        [Tooltip("Statistics from last generation")]
        private GenerationStats _lastStats = new GenerationStats();
        
        // Internal state
        private GameObject _generatedRoot;
        private string _lastGrammarName = "";
        private string _lastAxiomName = "";
        
        /// <summary>
        /// Current generation status message
        /// </summary>
        public string GenerationStatus => _generationStatus;
        
        /// <summary>
        /// Statistics from the last generation
        /// </summary>
        public GenerationStats LastStats => _lastStats;
        
        /// <summary>
        /// Whether a generation currently exists
        /// </summary>
        public bool HasGeneration => _generatedRoot != null;
        
        /// <summary>
        /// Generate procedural content from the configured grammar
        /// </summary>
        public void Generate()
        {
            if (grammarAsset == null)
            {
                _generationStatus = "Error: No grammar asset assigned";
                Debug.LogError("ProceduralGenerator: Grammar asset not assigned!");
                return;
            }
            
            if (spatialStrategy == null)
            {
                _generationStatus = "Error: No spatial strategy assigned";
                Debug.LogError("ProceduralGenerator: Spatial strategy not assigned!");
                return;
            }
            
            try
            {
                _generationStatus = "Generating...";
                Debug.Log($"=== ProceduralGenerator: Starting Generation ===");
                
                // Clear previous generation if requested
                if (autoClearPrevious && _generatedRoot != null)
                {
                    DestroyImmediate(_generatedRoot);
                    _generatedRoot = null;
                }
                
                // Step 1: Export grammar to .pgr format
                _generationStatus = "Compiling grammar...";
                var pgrContent = ExportToPGR(grammarAsset);
                
                // Step 2: Compile grammar
                var engine = new GrammarEngine();
                var grammarDef = engine.CompileFromSource(pgrContent, out string error);
                
                if (grammarDef == null)
                {
                    _generationStatus = $"Error: Grammar compilation failed - {error}";
                    Debug.LogError($"ProceduralGenerator: Failed to compile grammar: {error}");
                    return;
                }
                
                Debug.Log($"✓ Grammar compiled: {grammarDef.Symbols.Count} symbols, {grammarDef.Rules.Count} rules");
                
                // Step 3: Build parameter dictionary and populate strategy-provided values
                _generationStatus = "Preparing parameters...";
                var parameters = new Dictionary<string, object>();
                
                // Initialize spatial context to get strategy-provided values
                var tempContext = new SpatialContext();
                spatialStrategy.Initialize(tempContext);
                
                foreach (var param in axiomParameters)
                {
                    // Check if strategy provides this parameter
                    var strategyValue = tempContext.GetVariable<float>(param.name, float.MinValue);
                    if (strategyValue != float.MinValue)
                    {
                        // Use strategy-provided value
                        parameters[param.name] = strategyValue;
                        Debug.Log($"✓ Using strategy-provided parameter: {param.name} = {strategyValue}");
                    }
                    else
                    {
                        // Use user-specified value
                        parameters[param.name] = param.GetValue();
                    }
                }
                
                Debug.Log($"✓ Parameters: {string.Join(", ", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                
                // Step 4: Generate derivation tree
                _generationStatus = "Expanding grammar rules...";
                var derivationTree = engine.GenerateFromSymbol(grammarAsset.axiom, parameters, maxIterations);
                
                if (derivationTree == null)
                {
                    _generationStatus = "Error: Derivation tree generation failed";
                    Debug.LogError("ProceduralGenerator: Failed to generate derivation tree");
                    return;
                }
                
                int treeDepth = derivationTree.GetMaxDepth();
                int leafCount = derivationTree.GetLeafCount();
                Debug.Log($"✓ Derivation completed: {treeDepth} depth, {leafCount} leaves");
                
                // Step 5: Create spatial interpreter
                _generationStatus = "Interpreting spatial layout...";
                var interpreter = new SpatialInterpreter(spatialStrategy);
                
                // Register geometry library
                if (geometryLibrary != null && geometryLibrary.Length > 0)
                {
                    interpreter.RegisterGeometryLibrary(geometryLibrary);
                    Debug.Log($"✓ Registered {geometryLibrary.Length} geometry assets");
                }
                else
                {
                    Debug.LogWarning("⚠ No geometry library assigned - will use cube placeholders");
                }
                
                // Step 6: Interpret to spatial graph
                var spatialGraph = interpreter.Interpret(derivationTree);
                Debug.Log($"✓ Spatial graph created: {spatialGraph.NodesById.Count} nodes");
                
                // Write debug output if path is specified
                if (!string.IsNullOrEmpty(debugOutputPath))
                {
                    try
                    {
                        var debugContent = BuildDebugOutput(pgrContent, derivationTree, spatialGraph, parameters);
                        System.IO.File.WriteAllText(debugOutputPath, debugContent);
                        Debug.Log($"✓ Debug output written to: {debugOutputPath}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"⚠ Failed to write debug output: {ex.Message}");
                    }
                }
                
                // Step 7: Generate Unity meshes
                _generationStatus = "Generating meshes...";
                var meshGenerator = new MeshGenerator();
                _generatedRoot = meshGenerator.Generate(spatialGraph, transform, includeNonTerminals);
                
                // Update stats
                _lastStats = new GenerationStats
                {
                    grammarName = grammarAsset.grammarName,
                    symbolCount = grammarDef.Symbols.Count,
                    ruleCount = grammarDef.Rules.Count,
                    derivationDepth = treeDepth,
                    terminalCount = leafCount,
                    spatialNodeCount = spatialGraph.NodesById.Count,
                    generatedObjectCount = _generatedRoot.transform.childCount,
                    bounds = spatialGraph.Bounds
                };
                
                _generationStatus = $"Complete! Generated {_generatedRoot.transform.childCount} objects";
                Debug.Log($"✓ Generation complete!");
                Debug.Log($"  - Grammar: {grammarAsset.grammarName}");
                Debug.Log($"  - Objects: {_generatedRoot.transform.childCount}");
                Debug.Log($"  - Bounds: {spatialGraph.Bounds}");
            }
            catch (System.Exception e)
            {
                _generationStatus = $"Error: {e.Message}";
                Debug.LogError($"ProceduralGenerator: Generation failed: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Clear the currently generated content
        /// </summary>
        public void Clear()
        {
            if (_generatedRoot != null)
            {
                DestroyImmediate(_generatedRoot);
                _generatedRoot = null;
                _generationStatus = "Cleared";
                Debug.Log("ProceduralGenerator: Cleared generated content");
            }
        }
        
        /// <summary>
        /// Export grammar asset to .pgr format
        /// </summary>
        private string ExportToPGR(GrammarAsset asset)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"// Grammar: {asset.grammarName}");
            sb.AppendLine($"// Axiom: {asset.axiom}");
            sb.AppendLine($"// Max Iterations: {maxIterations}");
            sb.AppendLine();
            
            // Symbols
            sb.AppendLine("// === SYMBOLS ===");
            foreach (var symbol in asset.symbols)
            {
                sb.Append($"symbol {symbol.name}");
                if (symbol.parameters.Count > 0)
                {
                    sb.Append("(");
                    for (int i = 0; i < symbol.parameters.Count; i++)
                    {
                        var param = symbol.parameters[i];
                        sb.Append($"{param.name}:{ParameterTypeToString(param.type)}");
                        if (!string.IsNullOrEmpty(param.defaultValue))
                            sb.Append($"={param.defaultValue}");
                        if (i < symbol.parameters.Count - 1)
                            sb.Append(", ");
                    }
                    sb.Append(")");
                }
                sb.AppendLine(";");
            }
            sb.AppendLine();
            
            // Rules
            sb.AppendLine("// === RULES ===");
            foreach (var rule in asset.rules)
            {
                sb.AppendLine($"// Rule: {rule.name}");
                sb.AppendLine($"rule {rule.name}");
                sb.Append($"    when {rule.predecessor.name}");
                if (rule.predecessor.parameters.Count > 0)
                {
                    sb.Append("(");
                    for (int i = 0; i < rule.predecessor.parameters.Count; i++)
                    {
                        var param = rule.predecessor.parameters[i];
                        sb.Append(param.name);
                        if (i < rule.predecessor.parameters.Count - 1)
                            sb.Append(", ");
                    }
                    sb.Append(")");
                }
                sb.AppendLine();
                
                // Process each production
                foreach (var production in rule.productions)
                {
                    // Conditions
                    if (production.conditions.Count > 0)
                    {
                        sb.Append("    if (");
                        for (int c = 0; c < production.conditions.Count; c++)
                        {
                            var cond = production.conditions[c];
                            sb.Append($"{cond.leftOperand} {ConditionOperatorToString(cond.op)} {cond.rightOperand}");
                            if (c < production.conditions.Count - 1)
                                sb.Append(" && ");
                        }
                        sb.AppendLine(")");
                    }
                    
                    // Production steps
                    sb.Append("    => ");
                    for (int s = 0; s < production.steps.Count; s++)
                    {
                        var step = production.steps[s];
                        if (step.type == ProductionStepType.Symbol)
                        {
                            sb.Append(step.symbolName);
                            if (step.parameterAssignments.Count > 0)
                            {
                                sb.Append("(");
                                for (int a = 0; a < step.parameterAssignments.Count; a++)
                                {
                                    var assign = step.parameterAssignments[a];
                                    sb.Append($"{assign.parameterName} = {assign.valueExpression}");
                                    if (a < step.parameterAssignments.Count - 1)
                                        sb.Append(", ");
                                }
                                sb.Append(")");
                            }
                        }
                        if (s < production.steps.Count - 1)
                            sb.Append(" ");
                    }
                    sb.AppendLine(";");
                }
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
        
        private string ParameterTypeToString(ParameterType type)
        {
            return type switch
            {
                ParameterType.Float => "float",
                ParameterType.Int => "int",
                ParameterType.String => "string",
                ParameterType.Bool => "bool",
                ParameterType.Vector3 => "Vector3",
                ParameterType.Color => "Color",
                _ => "float"
            };
        }
        
        private string ConditionOperatorToString(ConditionOperator op)
        {
            return op switch
            {
                ConditionOperator.Equals => "==",
                ConditionOperator.NotEquals => "!=",
                ConditionOperator.LessThan => "<",
                ConditionOperator.LessOrEqual => "<=",
                ConditionOperator.GreaterThan => ">",
                ConditionOperator.GreaterOrEqual => ">=",
                _ => "=="
            };
        }
        
        /// <summary>
        /// Build debug output containing grammar, derivation tree, and spatial data
        /// </summary>
        private string BuildDebugOutput(string pgrContent, DerivationTree derivationTree, SpatialGraph spatialGraph, Dictionary<string, object> parameters)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine("=== PROCEDURAL GENERATION DEBUG OUTPUT ===");
            sb.AppendLine($"Generated: {System.DateTime.Now}");
            sb.AppendLine($"Strategy: {spatialStrategy.Name}");
            sb.AppendLine();
            
            // Parameters
            sb.AppendLine("=== AXIOM PARAMETERS ===");
            foreach (var kvp in parameters)
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            sb.AppendLine();
            
            // Grammar
            sb.AppendLine("=== GRAMMAR (.PGR) ===");
            sb.AppendLine(pgrContent);
            sb.AppendLine();
            
            // Derivation Tree
            sb.AppendLine("=== DERIVATION TREE ===");
            sb.AppendLine($"Depth: {derivationTree.GetMaxDepth()}");
            sb.AppendLine($"Leaves: {derivationTree.GetLeafCount()}");
            sb.AppendLine();
            PrintDerivationNode(sb, derivationTree.Root, 0);
            sb.AppendLine();
            
            // Spatial Graph
            sb.AppendLine("=== SPATIAL GRAPH ===");
            sb.AppendLine($"Total Nodes: {spatialGraph.NodesById.Count}");
            sb.AppendLine($"Root Node: {spatialGraph.Root?.SymbolName ?? "None"}");
            sb.AppendLine();
            
            foreach (var node in spatialGraph.NodesById.Values)
            {
                sb.AppendLine($"Node {node.Id}: {node.SymbolName}");
                sb.AppendLine($"  Position: {node.Position}");
                sb.AppendLine($"  Rotation: {node.Rotation.eulerAngles}");
                sb.AppendLine($"  Scale: {node.Scale}");
                sb.AppendLine($"  Children: {node.Children.Count}");
                sb.AppendLine($"  Has Geometry: {node.GeometryData != null}");
                if (node.Parameters.Count > 0)
                {
                    sb.Append("  Parameters: ");
                    sb.AppendLine(string.Join(", ", node.Parameters.Select(kvp => $"{kvp.Key}={kvp.Value}")));
                }
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
        
        private void PrintDerivationNode(System.Text.StringBuilder sb, DerivationNode node, int indent)
        {
            string indentStr = new string(' ', indent * 2);
            sb.Append(indentStr);
            sb.Append(node.Symbol.Type.Name);
            if (node.Symbol.Parameters != null && node.Symbol.Parameters.Count > 0)
            {
                sb.Append("(");
                sb.Append(string.Join(", ", node.Symbol.Parameters.Select(kvp => $"{kvp.Key}={kvp.Value}")));
                sb.Append(")");
            }
            sb.AppendLine();
            
            foreach (var child in node.Children)
            {
                PrintDerivationNode(sb, child, indent + 1);
            }
        }
        
        /// <summary>
        /// Update axiom parameters when grammar or axiom changes (called by editor)
        /// </summary>
        public void UpdateAxiomParameters()
        {
            if (grammarAsset == null) return;
            
            string currentGrammarName = grammarAsset.name;
            string currentAxiomName = grammarAsset.axiom;
            
            if (currentGrammarName != _lastGrammarName || currentAxiomName != _lastAxiomName)
            {
                _lastGrammarName = currentGrammarName;
                _lastAxiomName = currentAxiomName;
                RebuildParameterList();
            }
        }
        
        private void RebuildParameterList()
        {
            var axiomSymbol = grammarAsset.symbols.FirstOrDefault(s => s.name == grammarAsset.axiom);
            if (axiomSymbol == null || axiomSymbol.parameters == null)
            {
                axiomParameters.Clear();
                return;
            }
            
            // Preserve existing values where parameter names match
            var existingParams = axiomParameters.ToDictionary(p => p.name, p => p);
            axiomParameters.Clear();
            
            foreach (var param in axiomSymbol.parameters)
            {
                if (existingParams.TryGetValue(param.name, out var existing))
                {
                    axiomParameters.Add(existing);
                }
                else
                {
                    axiomParameters.Add(new AxiomParameter
                    {
                        name = param.name,
                        type = param.type
                    });
                }
            }
        }
    }
    
    /// <summary>
    /// Statistics from a generation run
    /// </summary>
    [System.Serializable]
    public class GenerationStats
    {
        public string grammarName;
        public int symbolCount;
        public int ruleCount;
        public int derivationDepth;
        public int terminalCount;
        public int spatialNodeCount;
        public int generatedObjectCount;
        public Bounds bounds;
        
        public override string ToString()
        {
            return $"Grammar: {grammarName}\n" +
                   $"Symbols: {symbolCount}, Rules: {ruleCount}\n" +
                   $"Derivation: {derivationDepth} depth, {terminalCount} terminals\n" +
                   $"Spatial: {spatialNodeCount} nodes\n" +
                   $"Generated: {generatedObjectCount} objects\n" +
                   $"Bounds: {bounds}";
        }
    }
}
