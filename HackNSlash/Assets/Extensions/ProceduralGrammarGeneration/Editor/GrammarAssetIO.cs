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
                // Add symbol description as comment if present
                if (!string.IsNullOrEmpty(symbol.description))
                {
                    foreach (var line in symbol.description.Split('\n'))
                    {
                        sb.AppendLine($"// {line.Trim()}");
                    }
                }
                
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
                        
                        // Add inline parameter comment if present
                        if (!string.IsNullOrEmpty(param.description))
                        {
                            sb.Append($" /* {param.description} */");
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
                // Add rule description as comment if present
                if (!string.IsNullOrEmpty(rule.description))
                {
                    foreach (var line in rule.description.Split('\n'))
                    {
                        sb.AppendLine($"// {line.Trim()}");
                    }
                }
                
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
                // Extract comments from source for documentation
                var commentMap = ExtractComments(pgrContent);
                
                // Use our existing parser to parse the content
                var lexer = new GrammarLexer(pgrContent);
                var tokens = lexer.Tokenize();
                var parser = new GrammarParser(tokens);
                var grammarDef = parser.Parse();
                
                // Set grammar metadata
                asset.grammarName = grammarDef.Name ?? "ImportedGrammar";
                
                // Set axiom - use first symbol if EntrySymbol not set
                if (grammarDef.EntrySymbol.Name != null && !string.IsNullOrEmpty(grammarDef.EntrySymbol.Name))
                {
                    asset.axiom = grammarDef.EntrySymbol.Name;
                }
                else if (grammarDef.Symbols.Count > 0)
                {
                    asset.axiom = grammarDef.Symbols[0].Type.Name;
                    Debug.LogWarning($"No axiom defined in grammar, defaulting to first symbol: {asset.axiom}");
                }
                else
                {
                    asset.axiom = "Start";
                    Debug.LogWarning("No symbols found in grammar, defaulting axiom to 'Start'");
                }
                
                // Convert to serialized format
                asset.symbols.Clear();
                asset.rules.Clear();
                
                // Convert symbols
                foreach (var symbolDef in grammarDef.Symbols)
                {
                    var serializedSymbol = new SerializedSymbol(symbolDef.Type.Name);
                    
                    // Try to find comment for this symbol
                    if (commentMap.TryGetValue($"symbol:{symbolDef.Type.Name}", out string symbolComment))
                    {
                        serializedSymbol.description = symbolComment;
                    }
                    
                    // Get parameter definitions from symbol definition
                    foreach (var param in symbolDef.Parameters)
                    {
                        var serializedParam = new SerializedParameter(
                            param.Name,
                            param.Type,
                            param.DefaultValue?.ToString() ?? ""
                        );
                        
                        // Try to find comment for this parameter
                        if (commentMap.TryGetValue($"param:{symbolDef.Type.Name}.{param.Name}", out string paramComment))
                        {
                            serializedParam.description = paramComment;
                        }
                        
                        serializedSymbol.parameters.Add(serializedParam);
                    }
                    
                    asset.symbols.Add(serializedSymbol);
                }
                
                // Convert rules
                foreach (var rule in grammarDef.Rules)
                {
                    var serializedRule = new SerializedRule(rule.Name ?? $"Rule_{rule.InputSymbol.Name}");
                    
                    // Try to find comment for this rule
                    if (commentMap.TryGetValue($"rule:{rule.Name ?? rule.InputSymbol.Name}", out string ruleComment))
                    {
                        serializedRule.description = ruleComment;
                    }
                    
                    serializedRule.predecessor = new SerializedSymbol(rule.InputSymbol.Name);
                    
                    // Copy parameters from rule
                    foreach (var param in rule.Parameters)
                    {
                        serializedRule.predecessor.parameters.Add(new SerializedParameter(
                            param.Name,
                            param.Type,
                            param.DefaultValue?.ToString() ?? ""
                        ));
                    }
                    
                    // Convert expansion to production
                    var production = new SerializedProduction();
                    production.weight = 1.0f;
                    
                    // Add condition if present
                    if (rule.Condition != null)
                    {
                        ConvertConditionToSerialized(rule.Condition, production.conditions);
                    }
                    
                    // Convert expansion to steps
                    if (rule.Expansion != null)
                    {
                        ConvertExpansionToSteps(rule.Expansion, production.steps);
                    }
                    else
                    {
                        Debug.LogWarning($"Rule '{rule.Name}' has no expansion");
                    }
                    
                    serializedRule.productions.Add(production);
                    asset.rules.Add(serializedRule);
                }
                
                Debug.Log($"Successfully imported grammar '{asset.grammarName}' with {asset.symbols.Count} symbols and {asset.rules.Count} rules (axiom: {asset.axiom})");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to import grammar: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }
        
        /// <summary>
        /// Extract comments from .pgr file and associate them with symbols/rules/parameters
        /// </summary>
        private static Dictionary<string, string> ExtractComments(string pgrContent)
        {
            var commentMap = new Dictionary<string, string>();
            var lines = pgrContent.Split('\n');
            var pendingComments = new List<string>();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Collect single-line comments
                if (line.StartsWith("//"))
                {
                    var comment = line.Substring(2).Trim();
                    // Skip section headers and "Rule:" markers
                    if (!comment.StartsWith("===") && !comment.StartsWith("Rule:") && 
                        !comment.StartsWith("Grammar:") && !comment.StartsWith("Axiom:") && 
                        !comment.StartsWith("Max Iterations:"))
                    {
                        pendingComments.Add(comment);
                    }
                    continue;
                }
                
                // Check for symbol or rule definitions
                if (line.StartsWith("symbol "))
                {
                    // Extract symbol name
                    var symbolName = line.Substring(7).Split('(', ';')[0].Trim();
                    if (pendingComments.Count > 0)
                    {
                        commentMap[$"symbol:{symbolName}"] = string.Join("\n", pendingComments);
                        pendingComments.Clear();
                    }
                    
                    // Extract inline parameter comments (/* ... */)
                    if (line.Contains("/*") && line.Contains("*/"))
                    {
                        var paramStart = line.IndexOf('(');
                        var paramEnd = line.IndexOf(')');
                        if (paramStart > 0 && paramEnd > paramStart)
                        {
                            var paramSection = line.Substring(paramStart + 1, paramEnd - paramStart - 1);
                            var paramParts = paramSection.Split(',');
                            foreach (var paramPart in paramParts)
                            {
                                if (paramPart.Contains("/*") && paramPart.Contains("*/"))
                                {
                                    var paramName = paramPart.Split(':')[0].Trim();
                                    var commentStart = paramPart.IndexOf("/*") + 2;
                                    var commentEnd = paramPart.IndexOf("*/");
                                    var comment = paramPart.Substring(commentStart, commentEnd - commentStart).Trim();
                                    commentMap[$"param:{symbolName}.{paramName}"] = comment;
                                }
                            }
                        }
                    }
                }
                else if (line.StartsWith("rule "))
                {
                    // Extract rule name
                    var ruleName = line.Substring(5).Split(' ', '\t')[0].Trim();
                    if (pendingComments.Count > 0)
                    {
                        commentMap[$"rule:{ruleName}"] = string.Join("\n", pendingComments);
                        pendingComments.Clear();
                    }
                }
                else if (!string.IsNullOrEmpty(line) && !line.StartsWith("//"))
                {
                    // Clear pending comments if we hit non-comment, non-definition line
                    pendingComments.Clear();
                }
            }
            
            return commentMap;
        }
        
        private static void ConvertConditionToSerialized(Condition condition, List<SerializedCondition> serializedConditions)
        {
            if (condition is ComparisonCondition comparison)
            {
                var serialized = new SerializedCondition();
                serialized.leftOperand = comparison.LeftOperand;
                serialized.rightOperand = comparison.RightOperand?.ToString() ?? "";
                serialized.op = comparison.Operator switch
                {
                    ComparisonOperator.Equal => ConditionOperator.Equals,
                    ComparisonOperator.NotEqual => ConditionOperator.NotEquals,
                    ComparisonOperator.GreaterThan => ConditionOperator.GreaterThan,
                    ComparisonOperator.LessThan => ConditionOperator.LessThan,
                    ComparisonOperator.GreaterOrEqual => ConditionOperator.GreaterOrEqual,
                    ComparisonOperator.LessOrEqual => ConditionOperator.LessOrEqual,
                    _ => ConditionOperator.Equals
                };
                serializedConditions.Add(serialized);
            }
            else if (condition is LogicalCondition logical)
            {
                // For AND conditions, add all operands
                if (logical.Operator == LogicalOperator.And)
                {
                    foreach (var operand in logical.Operands)
                    {
                        ConvertConditionToSerialized(operand, serializedConditions);
                    }
                }
            }
        }
        
        private static void ConvertExpansionToSteps(Expansion expansion, List<SerializedProductionStep> steps)
        {
            if (expansion is ProductionExpansion production)
            {
                // Simple symbol production
                foreach (var symbolInstance in production.Symbols)
                {
                    var step = new SerializedProductionStep();
                    step.type = ProductionStepType.Symbol;
                    step.symbolName = symbolInstance.Type.Name;
                    
                    // Convert parameter expressions
                    foreach (var paramExpr in symbolInstance.ParameterExpressions)
                    {
                        step.parameterAssignments.Add(new SerializedParameterAssignment(
                            paramExpr.Key,
                            ExpressionToString(paramExpr.Value)
                        ));
                    }
                    
                    steps.Add(step);
                }
            }
            else if (expansion is RepeatExpansion repeat)
            {
                // For now, just extract the content as a symbol
                // A full implementation would need to represent repeat in the UI
                ConvertExpansionToSteps(repeat.Content, steps);
            }
            else if (expansion is SplitExpansion split)
            {
                // For now, extract all parts
                foreach (var part in split.Parts)
                {
                    ConvertExpansionToSteps(part.Expansion, steps);
                }
            }
            else if (expansion is ChooseExpansion choose)
            {
                // Take the first option for simplification
                if (choose.Options.Count > 0)
                {
                    ConvertExpansionToSteps(choose.Options[0].Expansion, steps);
                }
            }
        }
        
        private static string ExpressionToString(Expression expr)
        {
            if (expr is LiteralExpression literal)
            {
                return literal.Value?.ToString() ?? "";
            }
            else if (expr is ParameterExpression param)
            {
                return param.ParameterName;
            }
            else if (expr is BinaryExpression binary)
            {
                var left = ExpressionToString(binary.Left);
                var right = ExpressionToString(binary.Right);
                var op = binary.Operator switch
                {
                    BinaryOperator.Add => "+",
                    BinaryOperator.Subtract => "-",
                    BinaryOperator.Multiply => "*",
                    BinaryOperator.Divide => "/",
                    _ => "+"
                };
                return $"{left} {op} {right}";
            }
            return "";
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
