using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ProceduralGrammarGeneration.GrammarParsing;
using ProceduralGrammarGeneration.Runtime;
using ProceduralGrammarGeneration.Spatial;
using ProceduralGrammarGeneration.Testing;

namespace ProceduralGrammarGeneration.Generation
{
    /// <summary>
    /// MonoBehaviour for testing the complete grammar -> spatial -> mesh generation pipeline.
    /// Generates actual Unity GameObjects with meshes from grammar rules.
    /// </summary>
    public class MeshTester : MonoBehaviour
    {
        [Header("Grammar")]
        [Tooltip("Grammar asset to test")]
        public GrammarAsset grammarAsset;
        
        [Header("Spatial Strategy")]
        
        [SerializeReference]
        [Tooltip("Type of spatial strategy to use for interpretation")]
        public BaseSpatialStrategy spatialStrategy;
        
        [Header("Geometry Library")]
        [Tooltip("Geometry data for terminal symbols (Window, Door, Wall, etc.)")]
        public SymbolGeometryData[] geometryLibrary;
        
        [Header("Generation Options")]
        [Tooltip("Include non-terminal nodes as GameObjects (for debugging hierarchy)")]
        public bool includeNonTerminals = false;
        
        [Tooltip("Clear previous generation before creating new one")]
        public bool clearPrevious = true;
        
        [Header("Parameters")]
        [Tooltip("Parameters to pass to the axiom symbol")]
        public List<AxiomParameter> axiomParameters = new List<AxiomParameter>();
        
        [TextArea(3, 6)]
        [Tooltip("Help: Add parameters that match the axiom symbol's definition.")]
        public string parameterHelp = "Add parameters matching your grammar's axiom symbol (e.g., length, width, height, windowSize, etc.)";
        
        private string _lastGrammarName = "";
        private string _lastAxiomName = "";
        private GameObject _generatedRoot;
        
        [ContextMenu("Generate Mesh")]
        public void GenerateMesh()
        {
            if (grammarAsset == null)
            {
                Debug.LogError("Grammar asset not assigned!");
                return;
            }
            
            try
            {
                Debug.Log($"=== Starting Mesh Generation: {grammarAsset.grammarName} ===");
                
                // Clear previous generation if requested
                if (clearPrevious && _generatedRoot != null)
                {
                    DestroyImmediate(_generatedRoot);
                    _generatedRoot = null;
                }
                
                // Step 1: Export grammar to .pgr format
                var pgrContent = ExportToPGR(grammarAsset);
                
                // Step 2: Compile grammar
                var engine = new GrammarEngine();
                var grammarDef = engine.CompileFromSource(pgrContent, out string error);
                
                if (grammarDef == null)
                {
                    Debug.LogError($"Failed to compile grammar: {error}");
                    return;
                }
                
                Debug.Log($"✓ Grammar compiled: {grammarDef.Symbols.Count} symbols, {grammarDef.Rules.Count} rules");
                
                // Step 3: Build parameter dictionary
                var parameters = new Dictionary<string, object>();
                foreach (var param in axiomParameters)
                {
                    parameters[param.name] = param.GetValue();
                }
                
                Debug.Log($"✓ Parameters: {string.Join(", ", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                
                // Step 4: Generate derivation tree
                var derivationTree = engine.GenerateFromSymbol(grammarAsset.axiom, parameters, grammarAsset.maxIterations);
                
                if (derivationTree == null)
                {
                    Debug.LogError("Failed to generate derivation tree");
                    return;
                }
                
                Debug.Log($"✓ Derivation completed: {derivationTree.GetMaxDepth()} depth, {derivationTree.GetLeafCount()} leaves");
                
                // Step 5: Create spatial interpreter with strategy
                ISpatialStrategy spatialStrategy = CreateStrategy();
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
                
                // Step 7: Generate Unity meshes
                var meshGenerator = new MeshGenerator();
                _generatedRoot = meshGenerator.Generate(spatialGraph, transform, includeNonTerminals);
                
                Debug.Log($"✓ Mesh generation complete!");
                Debug.Log($"Generated structure: {_generatedRoot.transform.childCount} children");
                Debug.Log($"Bounds: {spatialGraph.Bounds}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Mesh generation failed: {e.Message}\n{e.StackTrace}");
            }
        }
        
        [ContextMenu("Clear Generated Mesh")]
        public void ClearMesh()
        {
            if (_generatedRoot != null)
            {
                DestroyImmediate(_generatedRoot);
                _generatedRoot = null;
                Debug.Log("Cleared generated mesh");
            }
        }
        
        private ISpatialStrategy CreateStrategy()
        {
            return spatialStrategy;
        }
        
        private string ExportToPGR(GrammarAsset asset)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"// Grammar: {asset.grammarName}");
            sb.AppendLine($"// Axiom: {asset.axiom}");
            sb.AppendLine($"// Max Iterations: {asset.maxIterations}");
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
                        sb.Append(rule.predecessor.parameters[i].name);
                        if (i < rule.predecessor.parameters.Count - 1)
                            sb.Append(", ");
                    }
                    sb.Append(")");
                }
                sb.AppendLine();
                
                foreach (var production in rule.productions)
                {
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
        
        private void OnValidate()
        {
            // Only rebuild parameters when grammar or axiom changes
            string currentGrammarName = grammarAsset != null ? grammarAsset.name : "";
            string currentAxiomName = grammarAsset != null ? grammarAsset.axiom : "";
            
            if (currentGrammarName != _lastGrammarName || currentAxiomName != _lastAxiomName)
            {
                _lastGrammarName = currentGrammarName;
                _lastAxiomName = currentAxiomName;
                
                if (grammarAsset != null)
                {
                    RebuildParameterList();
                }
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
}
