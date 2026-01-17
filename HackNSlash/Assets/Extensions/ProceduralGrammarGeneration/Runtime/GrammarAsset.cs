using System.Collections.Generic;
using UnityEngine;
using ProceduralGrammarGeneration.GrammarParsing;

namespace ProceduralGrammarGeneration.Runtime
{
    /// <summary>
    /// Unity asset that stores a procedural grammar definition.
    /// Can be edited visually in the Unity Editor and exported to .pgr files.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGrammar", menuName = "Procedural Grammar/Grammar Asset", order = 1)]
    public class GrammarAsset : ScriptableObject
    {
        [Header("Grammar Definition")]
        [Tooltip("Name of this grammar")]
        public string grammarName = "NewGrammar";
        
        [Tooltip("Starting symbol for grammar derivation")]
        public string axiom = "Start";
        
        [Tooltip("Maximum derivation iterations")]
        public int maxIterations = 100;
        
        [Header("Symbols")]
        [Tooltip("All symbols defined in this grammar")]
        public List<SerializedSymbol> symbols = new List<SerializedSymbol>();
        
        [Header("Rules")]
        [Tooltip("Production rules for symbol expansion")]
        public List<SerializedRule> rules = new List<SerializedRule>();
        
        [Header("File Settings")]
        [Tooltip("Optional: Path to export/import .pgr file")]
        public string filePath = "";
        
        /// <summary>
        /// Find a symbol by name
        /// </summary>
        public SerializedSymbol FindSymbol(string symbolName)
        {
            return symbols.Find(s => s.name == symbolName);
        }
        
        /// <summary>
        /// Find a rule by name
        /// </summary>
        public SerializedRule FindRule(string ruleName)
        {
            return rules.Find(r => r.name == ruleName);
        }
        
        /// <summary>
        /// Add a new symbol with default settings
        /// </summary>
        public SerializedSymbol AddSymbol(string name = null)
        {
            var symbol = new SerializedSymbol(name ?? $"Symbol{symbols.Count + 1}");
            symbols.Add(symbol);
            return symbol;
        }
        
        /// <summary>
        /// Add a new rule with default settings
        /// </summary>
        public SerializedRule AddRule(string name = null)
        {
            var rule = new SerializedRule(name ?? $"Rule{rules.Count + 1}");
            rule.productions.Add(new SerializedProduction());
            rules.Add(rule);
            return rule;
        }
        
        /// <summary>
        /// Validate the grammar for common errors
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            
            // Check axiom exists
            if (string.IsNullOrEmpty(axiom))
            {
                errors.Add("Axiom is not defined");
            }
            else if (FindSymbol(axiom) == null)
            {
                errors.Add($"Axiom symbol '{axiom}' is not defined");
            }
            
            // Check for duplicate symbol names
            var symbolNames = new HashSet<string>();
            foreach (var symbol in symbols)
            {
                if (string.IsNullOrEmpty(symbol.name))
                {
                    errors.Add("Found symbol with empty name");
                }
                else if (symbolNames.Contains(symbol.name))
                {
                    errors.Add($"Duplicate symbol name: '{symbol.name}'");
                }
                else
                {
                    symbolNames.Add(symbol.name);
                }
            }
            
            // Check rules reference valid symbols
            foreach (var rule in rules)
            {
                if (rule.predecessor != null && !string.IsNullOrEmpty(rule.predecessor.name))
                {
                    if (FindSymbol(rule.predecessor.name) == null)
                    {
                        errors.Add($"Rule '{rule.name}' predecessor '{rule.predecessor.name}' is not a defined symbol");
                    }
                }
                
                // Check production steps reference valid symbols
                foreach (var production in rule.productions)
                {
                    foreach (var step in production.steps)
                    {
                        if (step.type == ProductionStepType.Symbol && !string.IsNullOrEmpty(step.symbolName))
                        {
                            if (FindSymbol(step.symbolName) == null)
                            {
                                errors.Add($"Rule '{rule.name}' references undefined symbol: '{step.symbolName}'");
                            }
                        }
                    }
                }
            }
            
            return errors;
        }
        
        private void OnValidate()
        {
            // Ensure at least one symbol exists
            if (symbols.Count == 0)
            {
                AddSymbol("Start");
            }
        }
    }
}
