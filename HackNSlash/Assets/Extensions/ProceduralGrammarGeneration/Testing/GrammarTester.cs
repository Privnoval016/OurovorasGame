using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ProceduralGrammarGeneration.GrammarParsing;
using ProceduralGrammarGeneration.Runtime;

namespace ProceduralGrammarGeneration.Testing
{
    /// <summary>
    /// Unity test script for validating the grammar system end-to-end.
    /// Allows testing grammar compilation, derivation, and parameter handling.
    /// </summary>
    public class GrammarTester : MonoBehaviour
    {
        [Header("Grammar Configuration")]
        [Tooltip("The grammar asset to test")]
        public GrammarAsset grammarAsset;
        
        [Tooltip("Output filename for the generated text")]
        public string outputFilename = "grammar_output.txt";
        
        [Header("Axiom Parameters")]
        [Tooltip("These parameters are automatically populated based on the axiom symbol of your grammar. Adjust values here before running the test.")]
        [SerializeField]
        private string parameterHelp = "Select a GrammarAsset above to see axiom parameters";
        
        [Tooltip("Parameter values for the starting symbol (axiom)")]
        public List<ParameterValue> axiomParameters = new List<ParameterValue>();
        
        [Header("Output")]
        [TextArea(10, 30)]
        public string lastGeneratedOutput = "";
        
        [System.Serializable]
        public class ParameterValue
        {
            public string name;
            public ParameterType type;
            
            // Value storage for different types
            public float floatValue;
            public int intValue;
            public string stringValue;
            public bool boolValue;
            public Vector3 vector3Value;
            public Color colorValue;
            
            public object GetValue()
            {
                return type switch
                {
                    ParameterType.Float => floatValue,
                    ParameterType.Int => intValue,
                    ParameterType.String => stringValue,
                    ParameterType.Bool => boolValue,
                    ParameterType.Vector3 => vector3Value,
                    ParameterType.Color => colorValue,
                    _ => null
                };
            }
            
            public void SetDefaultValue(object value)
            {
                if (value == null) return;
                
                switch (type)
                {
                    case ParameterType.Float:
                        floatValue = Convert.ToSingle(value);
                        break;
                    case ParameterType.Int:
                        intValue = Convert.ToInt32(value);
                        break;
                    case ParameterType.String:
                        stringValue = value.ToString();
                        break;
                    case ParameterType.Bool:
                        boolValue = Convert.ToBoolean(value);
                        break;
                    case ParameterType.Vector3:
                        if (value is Vector3 v3) vector3Value = v3;
                        break;
                    case ParameterType.Color:
                        if (value is Color c) colorValue = c;
                        break;
                }
            }
        }
        
        private void OnValidate()
        {
            // Update axiom parameters based on selected grammar asset
            if (grammarAsset != null && grammarAsset.symbols.Count > 0)
            {
                // Find the axiom symbol
                var axiomSymbol = grammarAsset.symbols.Find(s => s.name == grammarAsset.axiom);
                
                if (axiomSymbol != null)
                {
                    // Synchronize parameter list
                    var newParams = new List<ParameterValue>();
                    
                    foreach (var param in axiomSymbol.parameters)
                    {
                        // Check if parameter already exists
                        var existing = axiomParameters.Find(p => p.name == param.name);
                        
                        if (existing != null && existing.type == param.type)
                        {
                            // Keep existing value
                            newParams.Add(existing);
                        }
                        else
                        {
                            // Create new parameter
                            var newParam = new ParameterValue
                            {
                                name = param.name,
                                type = param.type
                            };
                            
                            // Parse default value if provided
                            if (!string.IsNullOrEmpty(param.defaultValue))
                            {
                                try
                                {
                                    switch (param.type)
                                    {
                                        case ParameterType.Float:
                                            newParam.floatValue = float.Parse(param.defaultValue);
                                            break;
                                        case ParameterType.Int:
                                            newParam.intValue = int.Parse(param.defaultValue);
                                            break;
                                        case ParameterType.String:
                                            newParam.stringValue = param.defaultValue.Trim('"');
                                            break;
                                        case ParameterType.Bool:
                                            newParam.boolValue = bool.Parse(param.defaultValue);
                                            break;
                                    }
                                }
                                catch
                                {
                                    // Use type defaults if parsing fails
                                }
                            }
                            
                            newParams.Add(newParam);
                        }
                    }
                    
                    axiomParameters = newParams;
                }
            }
        }
        
        [ContextMenu("Test Grammar Generation")]
        public void TestGrammarGeneration()
        {
            if (grammarAsset == null)
            {
                Debug.LogError("No grammar asset assigned!");
                return;
            }
            
            try
            {
                // Step 1: Export to PGR format  
                Debug.Log($"Exporting grammar '{grammarAsset.grammarName}'...");
                var pgrContent = ExportToPGR(grammarAsset);
                
                // Step 2: Compile grammar (simplified API)
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
                
                Debug.Log($"Starting derivation with axiom '{grammarAsset.axiom}' and parameters: {string.Join(", ", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                
                // Step 4: Generate (simplified API!)
                var derivationTree = engine.GenerateFromSymbol(grammarAsset.axiom, parameters, grammarAsset.maxIterations);
                
                if (derivationTree == null || derivationTree.Root == null)
                {
                    Debug.LogError("Derivation failed!");
                    return;
                }
                
                Debug.Log($"✓ Derivation completed. Tree depth: {derivationTree.Root.Depth}, Leaf nodes: {derivationTree.Root.GetLeaves().Count()}");

                
                // Step 5: Generate output text
                var output = GenerateOutputText(derivationTree, grammarDef);
                lastGeneratedOutput = output;
                
                // Step 6: Write to file
                var outputPath = System.IO.Path.Combine(Application.dataPath, "Extensions", "ProceduralGrammarGeneration", "Testing", outputFilename);
                System.IO.File.WriteAllText(outputPath, output);
                
                Debug.Log($"✓ Output written to: {outputPath}");
                Debug.Log($"\n--- Generated Output ---\n{output}\n--- End Output ---");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Grammar test failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private string GenerateOutputText(DerivationTree tree, GrammarDefinition grammarDef)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"=== Grammar Derivation Result ===");
            sb.AppendLine($"Grammar: {grammarAsset.grammarName}");
            sb.AppendLine($"Axiom: {grammarAsset.axiom}");
            sb.AppendLine($"Max Iterations: {grammarAsset.maxIterations}");
            
            var allNodes = new List<DerivationNode> { tree.Root };
            allNodes.AddRange(tree.Root.GetDescendants());
            sb.AppendLine($"Total Nodes: {allNodes.Count}");
            sb.AppendLine();
            
            // Show terminal vs non-terminal breakdown
            var terminals = tree.Root.GetLeaves().ToList();
            var nonTerminals = allNodes.Where(n => !n.IsLeaf).ToList();
            sb.AppendLine($"Non-terminal nodes (expanded): {nonTerminals.Count}");
            sb.AppendLine($"Terminal nodes (leaves): {terminals.Count}");
            sb.AppendLine();
            
            sb.AppendLine("=== Derivation Tree ===");
            PrintNode(sb, tree.Root, 0, grammarDef);
            
            sb.AppendLine();
            sb.AppendLine("=== Terminal Symbols (Final Output) ===");
            sb.AppendLine($"Total: {terminals.Count}");
            sb.AppendLine();
            
            for (int i = 0; i < terminals.Count; i++)
            {
                var terminal = terminals[i];
                sb.Append($"{i + 1}. {terminal.Symbol.Type.Name}");
                
                if (terminal.Symbol.Parameters.Count > 0)
                {
                    sb.Append(" [");
                    var paramList = new List<string>();
                    foreach (var kvp in terminal.Symbol.Parameters)
                    {
                        paramList.Add($"{kvp.Key}={kvp.Value}");
                    }
                    sb.Append(string.Join(", ", paramList));
                    sb.Append("]");
                }
                
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
        
        private void PrintNode(StringBuilder sb, DerivationNode node, int indent, GrammarDefinition grammarDef)
        {
            var indentStr = new string(' ', indent * 2);
            
            // Check if this symbol has rules (non-terminal) or is a leaf (terminal)
            var symbolDef = grammarDef.GetSymbol(node.Symbol.Type);
            var hasRules = grammarDef.Rules.Any(r => r.InputSymbol.Equals(node.Symbol.Type));
            var symbolType = node.IsLeaf ? "[TERMINAL]" : "[NON-TERMINAL]";
            
            sb.Append($"{indentStr}- {node.Symbol.Type.Name} {symbolType}");
            
            if (node.Symbol.Parameters.Count > 0)
            {
                sb.Append(" (");
                var paramList = new List<string>();
                foreach (var kvp in node.Symbol.Parameters)
                {
                    paramList.Add($"{kvp.Key}={kvp.Value}");
                }
                sb.Append(string.Join(", ", paramList));
                sb.Append(")");
            }
            
            sb.AppendLine($" [{node.Status}]");
            
            foreach (var child in node.Children)
            {
                PrintNode(sb, child, indent + 1, grammarDef);
            }
        }
        
        private List<DerivationNode> CollectTerminals(DerivationNode node)
        {
            return node.GetLeaves().ToList();
        }
        
        // Simplified export - delegates to GrammarAssetIO in editor
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
    }
}
