using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using ProceduralGrammarGeneration.GrammarParsing;

namespace ProceduralGrammarGeneration.Editor
{
    /// <summary>
    /// Bridges between GrammarAsset (Unity-serializable) and the backend grammar system.
    /// Handles conversion to/from .pgr text format and compilation.
    /// </summary>
    public static class GrammarAssetIO
    {
        /// <summary>
        /// Export a GrammarAsset to .pgr text format
        /// </summary>
        public static string ExportToPGR(GrammarAsset asset)
        {
            var sb = new StringBuilder();
            
            // Header comment
            sb.AppendLine($"// Grammar: {asset.grammarName}");
            sb.AppendLine($"// Axiom: {asset.axiom}");
            sb.AppendLine($"// Max Iterations: {asset.maxIterations}");
            sb.AppendLine();
            
            // Symbols section
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
                        {
                            sb.Append($"={param.defaultValue}");
                        }
                        
                        if (i < symbol.parameters.Count - 1)
                            sb.Append(", ");
                    }
                    sb.Append(")");
                }
                
                sb.AppendLine(";");
            }
            sb.AppendLine();
            
            // Rules section
            sb.AppendLine("// === RULES ===");
            foreach (var rule in asset.rules)
            {
                sb.AppendLine($"// Rule: {rule.name}");
                
                // Predecessor
                sb.Append(rule.predecessor.name);
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
                
                sb.AppendLine(" -> ");
                
                // Productions
                for (int p = 0; p < rule.productions.Count; p++)
                {
                    var production = rule.productions[p];
                    
                    // Weight
                    if (rule.productions.Count > 1)
                    {
                        sb.Append($"    [{production.weight}] ");
                    }
                    else
                    {
                        sb.Append("    ");
                    }
                    
                    // Conditions
                    if (production.conditions.Count > 0)
                    {
                        sb.Append("if (");
                        for (int c = 0; c < production.conditions.Count; c++)
                        {
                            var cond = production.conditions[c];
                            sb.Append($"{cond.leftOperand} {ConditionOperatorToString(cond.op)} {cond.rightOperand}");
                            if (c < production.conditions.Count - 1)
                                sb.Append(" && ");
                        }
                        sb.Append(") ");
                    }
                    
                    // Steps
                    for (int s = 0; s < production.steps.Count; s++)
                    {
                        var step = production.steps[s];
                        
                        switch (step.type)
                        {
                            case ProductionStepType.Symbol:
                                sb.Append(step.symbolName);
                                if (step.parameterAssignments.Count > 0)
                                {
                                    sb.Append("(");
                                    for (int a = 0; a < step.parameterAssignments.Count; a++)
                                    {
                                        var assign = step.parameterAssignments[a];
                                        sb.Append($"{assign.parameterName}={assign.valueExpression}");
                                        if (a < step.parameterAssignments.Count - 1)
                                            sb.Append(", ");
                                    }
                                    sb.Append(")");
                                }
                                break;
                                
                            case ProductionStepType.SetParameter:
                                sb.Append($"set({step.operationTarget}, {step.operationValue})");
                                break;
                                
                            case ProductionStepType.ModifyParameter:
                                sb.Append($"{step.operationTarget} {OperationTypeToString(step.operationType)} {step.operationValue}");
                                break;
                        }
                        
                        if (s < production.steps.Count - 1)
                            sb.Append(" ");
                    }
                    
                    if (p < rule.productions.Count - 1)
                        sb.AppendLine();
                }
                
                sb.AppendLine(";");
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Load a .pgr file into a GrammarAsset
        /// </summary>
        public static void ImportFromPGR(GrammarAsset asset, string pgrContent)
        {
            try
            {
                // Use our existing parser to parse the content
                var lexer = new GrammarLexer(pgrContent);
                var tokens = lexer.Tokenize();
                var parser = new GrammarParser(tokens);
                var grammarDef = parser.Parse();
                
                // Convert to serialized format
                asset.symbols.Clear();
                asset.rules.Clear();
                
                // Convert symbols
                foreach (var symbolDef in grammarDef.Symbols)
                {
                    var serializedSymbol = new SerializedSymbol(symbolDef.Type.Name);
                    
                    // Get parameter definitions from symbol definition
                    foreach (var param in symbolDef.Parameters)
                    {
                        serializedSymbol.parameters.Add(new SerializedParameter(
                            param.Name,
                            param.Type,
                            param.DefaultValue?.ToString() ?? ""
                        ));
                    }
                    
                    /*
                    // Alternative: Get from first rule
                    foreach (var rule in grammarDef.Rules)
                    {
                        if (rule.InputSymbol.Name == symbolDef.Type.Name)
                        {
                            foreach (var param in rule.Parameters)
                            {
                                serializedSymbol.parameters.Add(new SerializedParameter(
                                    param.Name,
                                    param.Type,
                                    param.DefaultValue?.ToString() ?? ""
                                ));
                            }
                            break;
                        }
                    }
                    */
                    
                    asset.symbols.Add(serializedSymbol);
                }
                
                // Convert rules (simplified - full conversion would need more complex logic)
                foreach (var rule in grammarDef.Rules)
                {
                    var serializedRule = new SerializedRule($"Rule_{rule.InputSymbol.Name}");
                    serializedRule.predecessor = new SerializedSymbol(rule.InputSymbol.Name);
                    
                    foreach (var param in rule.Parameters)
                    {
                        serializedRule.predecessor.parameters.Add(new SerializedParameter(
                            param.Name,
                            param.Type,
                            param.DefaultValue?.ToString() ?? ""
                        ));
                    }
                    
                    // Note: Full production conversion would require parsing the production expressions
                    // This is a simplified version
                    var production = new SerializedProduction();
                    production.weight = 1.0f;
                    serializedRule.productions.Add(production);
                    
                    asset.rules.Add(serializedRule);
                }
                
                Debug.Log($"Successfully imported grammar with {asset.symbols.Count} symbols and {asset.rules.Count} rules");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to import grammar: {e.Message}");
            }
        }
        
        /// <summary>
        /// Compile a GrammarAsset using the backend engine
        /// </summary>
        public static GrammarEngine CompileGrammar(GrammarAsset asset, IGrammarFileReader fileReader = null)
        {
            var pgrContent = ExportToPGR(asset);
            
            var engine = new GrammarEngine(fileReader);
            if (!engine.CompileGrammar(pgrContent, out string errorMessage))
            {
                throw new Exception($"Failed to compile grammar: {errorMessage}");
            }
            
            return engine;
        }
        
        // Helper methods for string conversion
        private static string ParameterTypeToString(ParameterType type)
        {
            return type.ToString().ToLower();
        }
        
        private static string ConditionOperatorToString(ConditionOperator op)
        {
            switch (op)
            {
                case ConditionOperator.Equals: return "==";
                case ConditionOperator.NotEquals: return "!=";
                case ConditionOperator.GreaterThan: return ">";
                case ConditionOperator.LessThan: return "<";
                case ConditionOperator.GreaterOrEqual: return ">=";
                case ConditionOperator.LessOrEqual: return "<=";
                default: return "==";
            }
        }
        
        private static string OperationTypeToString(OperationType op)
        {
            switch (op)
            {
                case OperationType.Set: return "=";
                case OperationType.Add: return "+=";
                case OperationType.Multiply: return "*=";
                case OperationType.Subtract: return "-=";
                case OperationType.Divide: return "/=";
                default: return "=";
            }
        }
    }
}
