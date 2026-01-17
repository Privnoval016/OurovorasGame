using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProceduralGrammarGeneration.GrammarParsing;
using ProceduralGrammarGeneration.Runtime;

namespace ProceduralGrammarGeneration.Editor
{
    /// <summary>
    /// Provides functionality for composing multiple grammars together.
    /// Allows creating modular, reusable grammar components.
    /// </summary>
    public static class GrammarComposer
    {
        /// <summary>
        /// Merge multiple grammar assets into a single composed grammar.
        /// Later grammars can reference symbols from earlier ones.
        /// </summary>
        public static GrammarAsset MergeGrammars(string composedName, params GrammarAsset[] grammars)
        {
            if (grammars == null || grammars.Length == 0)
            {
                Debug.LogError("No grammars provided to merge");
                return null;
            }
            
            var composed = ScriptableObject.CreateInstance<GrammarAsset>();
            composed.grammarName = composedName;
            
            // Use the first grammar's axiom as default
            composed.axiom = grammars[0].axiom;
            composed.maxIterations = grammars[0].maxIterations;
            
            // Track symbol names to avoid duplicates
            var symbolNames = new HashSet<string>();
            
            // Merge all symbols
            foreach (var grammar in grammars)
            {
                foreach (var symbol in grammar.symbols)
                {
                    if (!symbolNames.Contains(symbol.name))
                    {
                        // Deep copy the symbol
                        var copiedSymbol = new SerializedSymbol(symbol.name);
                        foreach (var param in symbol.parameters)
                        {
                            copiedSymbol.parameters.Add(new SerializedParameter(
                                param.name, 
                                param.type, 
                                param.defaultValue
                            ));
                        }
                        composed.symbols.Add(copiedSymbol);
                        symbolNames.Add(symbol.name);
                    }
                    else
                    {
                        Debug.LogWarning($"Symbol '{symbol.name}' already exists, skipping duplicate from grammar '{grammar.grammarName}'");
                    }
                }
            }
            
            // Merge all rules
            foreach (var grammar in grammars)
            {
                foreach (var rule in grammar.rules)
                {
                    // Deep copy the rule
                    var copiedRule = new SerializedRule(rule.name);
                    
                    // Copy predecessor
                    copiedRule.predecessor = new SerializedSymbol(rule.predecessor.name);
                    foreach (var param in rule.predecessor.parameters)
                    {
                        copiedRule.predecessor.parameters.Add(new SerializedParameter(
                            param.name, 
                            param.type, 
                            param.defaultValue
                        ));
                    }
                    
                    // Copy productions
                    foreach (var production in rule.productions)
                    {
                        var copiedProduction = new SerializedProduction();
                        copiedProduction.weight = production.weight;
                        
                        // Copy conditions
                        foreach (var condition in production.conditions)
                        {
                            copiedProduction.conditions.Add(new SerializedCondition
                            {
                                leftOperand = condition.leftOperand,
                                op = condition.op,
                                rightOperand = condition.rightOperand
                            });
                        }
                        
                        // Copy steps
                        foreach (var step in production.steps)
                        {
                            var copiedStep = new SerializedProductionStep();
                            copiedStep.type = step.type;
                            copiedStep.symbolName = step.symbolName;
                            copiedStep.operationTarget = step.operationTarget;
                            copiedStep.operationValue = step.operationValue;
                            copiedStep.operationType = step.operationType;
                            
                            foreach (var assignment in step.parameterAssignments)
                            {
                                copiedStep.parameterAssignments.Add(new SerializedParameterAssignment(
                                    assignment.parameterName,
                                    assignment.valueExpression
                                ));
                            }
                            
                            copiedProduction.steps.Add(copiedStep);
                        }
                        
                        copiedRule.productions.Add(copiedProduction);
                    }
                    
                    composed.rules.Add(copiedRule);
                }
            }
            
            Debug.Log($"Merged {grammars.Length} grammars into '{composedName}': {composed.symbols.Count} symbols, {composed.rules.Count} rules");
            
            return composed;
        }
        
        /// <summary>
        /// Extend a base grammar with additional symbols and rules.
        /// Useful for adding specialized behavior to existing grammars.
        /// </summary>
        public static GrammarAsset ExtendGrammar(GrammarAsset baseGrammar, GrammarAsset extension, string extendedName = null)
        {
            var name = extendedName ?? $"{baseGrammar.grammarName}_Extended";
            return MergeGrammars(name, baseGrammar, extension);
        }
        
        /// <summary>
        /// Create a composed grammar that references symbols from multiple source grammars.
        /// This creates a new grammar with rules that can use symbols from all sources.
        /// </summary>
        public static GrammarAsset ComposeWithReferences(string composedName, string axiom, GrammarAsset[] sourceGrammars, List<SerializedRule> compositionRules)
        {
            // First merge all source grammars to get symbols
            var composed = MergeGrammars(composedName, sourceGrammars);
            
            // Override axiom
            composed.axiom = axiom;
            
            // Add composition rules
            foreach (var rule in compositionRules)
            {
                // Deep copy the rule
                var copiedRule = new SerializedRule(rule.name);
                copiedRule.predecessor = new SerializedSymbol(rule.predecessor.name);
                
                foreach (var param in rule.predecessor.parameters)
                {
                    copiedRule.predecessor.parameters.Add(new SerializedParameter(
                        param.name, 
                        param.type, 
                        param.defaultValue
                    ));
                }
                
                foreach (var production in rule.productions)
                {
                    var copiedProduction = new SerializedProduction();
                    copiedProduction.weight = production.weight;
                    
                    foreach (var condition in production.conditions)
                    {
                        copiedProduction.conditions.Add(new SerializedCondition
                        {
                            leftOperand = condition.leftOperand,
                            op = condition.op,
                            rightOperand = condition.rightOperand
                        });
                    }
                    
                    foreach (var step in production.steps)
                    {
                        var copiedStep = new SerializedProductionStep();
                        copiedStep.type = step.type;
                        copiedStep.symbolName = step.symbolName;
                        copiedStep.operationTarget = step.operationTarget;
                        copiedStep.operationValue = step.operationValue;
                        copiedStep.operationType = step.operationType;
                        
                        foreach (var assignment in step.parameterAssignments)
                        {
                            copiedStep.parameterAssignments.Add(new SerializedParameterAssignment(
                                assignment.parameterName,
                                assignment.valueExpression
                            ));
                        }
                        
                        copiedProduction.steps.Add(copiedStep);
                    }
                    
                    copiedRule.productions.Add(copiedProduction);
                }
                
                composed.rules.Add(copiedRule);
            }
            
            return composed;
        }
        
        /// <summary>
        /// Validate that all symbol references in the grammar are defined.
        /// Useful for checking composed grammars.
        /// </summary>
        public static List<string> ValidateSymbolReferences(GrammarAsset grammar)
        {
            var errors = new List<string>();
            var symbolNames = new HashSet<string>(grammar.symbols.Select(s => s.name));
            
            foreach (var rule in grammar.rules)
            {
                // Check predecessor
                if (!symbolNames.Contains(rule.predecessor.name))
                {
                    errors.Add($"Rule '{rule.name}' references undefined predecessor symbol: '{rule.predecessor.name}'");
                }
                
                // Check production steps
                foreach (var production in rule.productions)
                {
                    foreach (var step in production.steps)
                    {
                        if (step.type == ProductionStepType.Symbol && !string.IsNullOrEmpty(step.symbolName))
                        {
                            if (!symbolNames.Contains(step.symbolName))
                            {
                                errors.Add($"Rule '{rule.name}' production references undefined symbol: '{step.symbolName}'");
                            }
                        }
                    }
                }
            }
            
            return errors;
        }
        
        /// <summary>
        /// Get all symbols that are referenced but not defined in the grammar.
        /// Useful for identifying dependencies on external grammars.
        /// </summary>
        public static List<string> GetMissingSymbols(GrammarAsset grammar)
        {
            var defined = new HashSet<string>(grammar.symbols.Select(s => s.name));
            var referenced = new HashSet<string>();
            
            foreach (var rule in grammar.rules)
            {
                referenced.Add(rule.predecessor.name);
                
                foreach (var production in rule.productions)
                {
                    foreach (var step in production.steps)
                    {
                        if (step.type == ProductionStepType.Symbol && !string.IsNullOrEmpty(step.symbolName))
                        {
                            referenced.Add(step.symbolName);
                        }
                    }
                }
            }
            
            return referenced.Where(r => !defined.Contains(r)).ToList();
        }
    }
}
