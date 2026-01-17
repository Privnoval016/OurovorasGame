using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ProceduralGrammarGeneration.GrammarParsing;
using ProceduralGrammarGeneration.Runtime;
using ProceduralGrammarGeneration.Spatial;

namespace ProceduralGrammarGeneration.Testing
{
    /// <summary>
    /// Tests spatial interpretation - converts derivation tree to positioned 3D nodes.
    /// Outputs terminal symbol positions and rotations.
    /// </summary>
    public class SpatialTester : MonoBehaviour
    {
        [Header("Grammar")]
        [Tooltip("Grammar asset to test")]
        public GrammarAsset grammarAsset;
        
        [Header("Geometry Library")]
        [Tooltip("Geometry data for terminal symbols (Window, Door, Wall, FloorMarker, etc.)")]
        public SymbolGeometryData[] geometryLibrary;
        
        [Header("Spatial Settings")]
        [Tooltip("Height of each floor in meters")]
        public float heightPerFloor = 25f;
        
        [Header("Output")]
        [Tooltip("File to write spatial output to")]
        public string outputFile = "Testing/spatial_output.txt";
        
        [Header("Parameters")]
        [Tooltip("Parameters to pass to the axiom symbol")]
        public List<AxiomParameter> axiomParameters = new List<AxiomParameter>();
        
        [TextArea(3, 6)]
        [Tooltip("Help: Add parameters that match the axiom symbol's definition. E.g., for Facade(width, height, floors), add 3 parameters with those names.")]
        public string parameterHelp = "Add parameters matching your grammar's axiom symbol.";

        [ContextMenu("Test Spatial Generation")]
        public void TestSpatialGeneration()
        {
            if (grammarAsset == null)
            {
                Debug.LogError("Grammar asset not assigned!");
                return;
            }
            
            try
            {
                Debug.Log($"=== Testing Spatial Generation: {grammarAsset.grammarName} ===");
                
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
                
                Debug.Log($"✓ Grammar compiled successfully. {grammarDef.Symbols.Count} symbols, {grammarDef.Rules.Count} rules.");
                
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
                
                Debug.Log($"✓ Derivation completed. Tree depth: {derivationTree.GetMaxDepth()}, Leaf nodes: {derivationTree.GetLeafCount()}");
                
                // Step 5: Create spatial interpreter
                var strategy = new VerticalStackStrategy(heightPerFloor);
                var interpreter = new SpatialInterpreter(strategy);
                
                // Register geometry library
                if (geometryLibrary != null && geometryLibrary.Length > 0)
                {
                    interpreter.RegisterGeometryLibrary(geometryLibrary);
                    Debug.Log($"✓ Registered {geometryLibrary.Length} geometry data assets");
                }
                else
                {
                    Debug.LogWarning("⚠ No geometry library assigned - using default dimensions");
                }
                
                // Step 6: Interpret to spatial graph
                var spatialGraph = interpreter.Interpret(derivationTree);
                
                Debug.Log($"✓ Spatial graph created: {spatialGraph.NodesById.Count} nodes");
                
                // Step 7: Generate output text
                string outputText = GenerateSpatialOutput(spatialGraph, grammarDef);
                
                // Step 8: Write to file
                string fullPath = System.IO.Path.Combine(Application.dataPath, "..", "Assets", "Extensions", "ProceduralGrammarGeneration", outputFile);
                System.IO.File.WriteAllText(fullPath, outputText);
                
                Debug.Log($"✓ Spatial output written to: {outputFile}");
                Debug.Log($"\n{outputText}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Spatial test failed: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private string GenerateSpatialOutput(SpatialGraph graph, GrammarDefinition grammarDef)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("=== Spatial Interpretation Result ===");
            sb.AppendLine($"Grammar: {grammarAsset.grammarName}");
            sb.AppendLine($"Axiom: {grammarAsset.axiom}");
            sb.AppendLine($"Total Nodes: {graph.NodesById.Count}");
            sb.AppendLine($"Bounds: {graph.Bounds}");
            sb.AppendLine();
            
            // Get stats
            var stats = graph.GetStats();
            sb.AppendLine($"Terminal nodes (leaves): {stats.TerminalNodes}");
            sb.AppendLine($"Max depth: {stats.MaxDepth}");
            sb.AppendLine();
            
            sb.AppendLine("=== Symbol Distribution ===");
            foreach (var kvp in stats.SymbolCounts.OrderByDescending(kvp => kvp.Value))
            {
                bool isTerminal = grammarDef.Symbols.Any(s => s.Type.Name == kvp.Key && s.IsTerminal);
                string terminalLabel = isTerminal ? "[TERMINAL]" : "[NON-TERMINAL]";
                sb.AppendLine($"{kvp.Key} {terminalLabel}: {kvp.Value}");
            }
            sb.AppendLine();
            
            // Get all terminal nodes
            var terminalNodes = graph.GetTerminalNodes();
            
            sb.AppendLine($"=== Terminal Symbol Positions ({terminalNodes.Count} nodes) ===");
            sb.AppendLine();
            
            // Group by symbol type
            var groupedBySymbol = terminalNodes.GroupBy(n => n.SymbolName);
            
            foreach (var group in groupedBySymbol.OrderBy(g => g.Key))
            {
                sb.AppendLine($"--- {group.Key} ({group.Count()} instances) ---");
                
                int index = 1;
                foreach (var node in group)
                {
                    sb.AppendLine($"{index}. {node.SymbolName}");
                    sb.AppendLine($"   Position: {FormatVector3(node.Position)}");
                    sb.AppendLine($"   Rotation: {FormatQuaternion(node.Rotation)}");
                    sb.AppendLine($"   Scale: {FormatVector3(node.Scale)}");
                    
                    // Show parameters
                    if (node.Parameters.Count > 0)
                    {
                        sb.AppendLine($"   Parameters: {string.Join(", ", node.Parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                    }
                    
                    // Show geometry data info
                    if (node.GeometryData != null)
                    {
                        sb.AppendLine($"   Geometry: {node.GeometryData.name} (physical size: {node.GeometryData.GetPhysicalSize()})");
                    }
                    
                    sb.AppendLine();
                    index++;
                }
            }
            
            return sb.ToString();
        }
        
        private string FormatVector3(Vector3 v)
        {
            return $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
        }
        
        private string FormatQuaternion(Quaternion q)
        {
            Vector3 euler = q.eulerAngles;
            return $"({q.x:F3}, {q.y:F3}, {q.z:F3}, {q.w:F3}) [Euler: {FormatVector3(euler)}]";
        }
        
        private string ExportToPGR(GrammarAsset asset)
        {
            var sb = new StringBuilder();
            
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
            if (grammarAsset != null)
            {
                // Auto-populate parameters based on axiom symbol
                var axiomSymbol = grammarAsset.FindSymbol(grammarAsset.axiom);
                if (axiomSymbol != null && axiomSymbol.parameters.Count > 0)
                {
                    // Sync parameters
                    foreach (var param in axiomSymbol.parameters)
                    {
                        if (!axiomParameters.Any(p => p.name == param.name))
                        {
                            axiomParameters.Add(new AxiomParameter
                            {
                                name = param.name,
                                type = param.type,
                                floatValue = 0f,
                                intValue = 0
                            });
                        }
                    }
                }
            }
        }
    }
    
    [System.Serializable]
    public class AxiomParameter
    {
        public string name;
        public ParameterType type;
        public float floatValue;
        public int intValue;
        public string stringValue;
        public bool boolValue;
        
        public object GetValue()
        {
            return type switch
            {
                ParameterType.Float => floatValue,
                ParameterType.Int => intValue,
                ParameterType.String => stringValue,
                ParameterType.Bool => boolValue,
                _ => floatValue
            };
        }
    }
}
