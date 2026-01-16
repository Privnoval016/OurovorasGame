using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralGrammarGeneration.GrammarParsing
{
    /// <summary>
    /// Performs semantic analysis on a parsed grammar.
    /// This is the critical stage that validates meaning, resolves symbols,
    /// detects conflicts, and ensures correctness before execution.
    /// </summary>
    public class SemanticAnalyzer
    {
        private GrammarDefinition _grammar;
        private List<SemanticError> _errors;
        private List<SemanticWarning> _warnings;

        public SemanticAnalyzer(GrammarDefinition grammar)
        {
            _grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            _errors = new List<SemanticError>();
            _warnings = new List<SemanticWarning>();
        }

        /// <summary>
        /// Performs full semantic analysis and returns whether the grammar is valid.
        /// </summary>
        public SemanticAnalysisResult Analyze()
        {
            _errors.Clear();
            _warnings.Clear();

            // Phase 1: Symbol Resolution
            ValidateSymbolReferences();

            // Phase 2: Parameter Validation
            ValidateParameterUsage();

            // Phase 3: Rule Conflict Detection
            DetectRuleConflicts();

            // Phase 4: Choice Validation
            ValidateChoiceBlocks();

            // Phase 5: Recursion & Termination Analysis
            AnalyzeRecursion();

            // Phase 6: Expression Type Checking
            ValidateExpressions();

            return new SemanticAnalysisResult
            {
                IsValid = _errors.Count == 0,
                Errors = _errors.ToList(),
                Warnings = _warnings.ToList()
            };
        }

        #region Symbol Resolution

        private void ValidateSymbolReferences()
        {
            var symbolNames = new HashSet<string>(_grammar.Symbols.Select(s => s.Type.Name));

            foreach (var rule in _grammar.Rules)
            {
                // Validate input symbol exists
                if (!symbolNames.Contains(rule.InputSymbol.Name))
                {
                    AddError($"Rule '{rule.Name}' references undefined input symbol '{rule.InputSymbol.Name}'", rule);
                }

                // Validate all symbol instances in expansion
                ValidateExpansionSymbols(rule.Expansion, symbolNames, rule);
            }
        }

        private void ValidateExpansionSymbols(Expansion expansion, HashSet<string> symbolNames, Rule rule)
        {
            switch (expansion)
            {
                case ProductionExpansion prod:
                    foreach (var symbol in prod.Symbols)
                    {
                        if (!symbolNames.Contains(symbol.Type.Name))
                        {
                            AddError($"Rule '{rule.Name}' produces undefined symbol '{symbol.Type.Name}'", rule);
                        }
                    }
                    break;

                case SplitExpansion split:
                    foreach (var part in split.Parts)
                        ValidateExpansionSymbols(part.Expansion, symbolNames, rule);
                    break;

                case RepeatExpansion repeat:
                    ValidateExpansionSymbols(repeat.Content, symbolNames, rule);
                    break;

                case ChooseExpansion choose:
                    foreach (var option in choose.Options)
                        ValidateExpansionSymbols(option.Expansion, symbolNames, rule);
                    break;
            }
        }

        #endregion

        #region Parameter Validation

        private void ValidateParameterUsage()
        {
            foreach (var rule in _grammar.Rules)
            {
                var inputSymbol = _grammar.GetSymbol(rule.InputSymbol);
                if (inputSymbol == null) continue;

                // Build available parameters context
                var availableParams = new HashSet<string>(rule.Parameters.Select(p => p.Name));

                // Validate condition uses valid parameters
                if (rule.Condition != null)
                {
                    ValidateConditionParameters(rule.Condition, availableParams, rule);
                }

                // Validate expansion parameter usage
                ValidateExpansionParameters(rule.Expansion, availableParams, rule);
            }
        }

        private void ValidateConditionParameters(Condition condition, HashSet<string> availableParams, Rule rule)
        {
            switch (condition)
            {
                case ComparisonCondition comp:
                    if (!availableParams.Contains(comp.LeftOperand))
                    {
                        AddError($"Rule '{rule.Name}' condition references undefined parameter '{comp.LeftOperand}'", rule);
                    }
                    break;

                case LogicalCondition logical:
                    foreach (var operand in logical.Operands)
                        ValidateConditionParameters(operand, availableParams, rule);
                    break;
            }
        }

        private void ValidateExpansionParameters(Expansion expansion, HashSet<string> availableParams, Rule rule)
        {
            switch (expansion)
            {
                case ProductionExpansion prod:
                    foreach (var symbol in prod.Symbols)
                    {
                        foreach (var expr in symbol.ParameterExpressions.Values)
                        {
                            ValidateExpressionParameters(expr, availableParams, rule);
                        }
                    }
                    break;

                case SplitExpansion split:
                    foreach (var part in split.Parts)
                    {
                        ValidateExpressionParameters(part.Size, availableParams, rule);
                        ValidateExpansionParameters(part.Expansion, availableParams, rule);
                    }
                    break;

                case RepeatExpansion repeat:
                    ValidateExpressionParameters(repeat.Size, availableParams, rule);
                    ValidateExpansionParameters(repeat.Content, availableParams, rule);
                    break;

                case ChooseExpansion choose:
                    foreach (var option in choose.Options)
                        ValidateExpansionParameters(option.Expansion, availableParams, rule);
                    break;
            }
        }

        private void ValidateExpressionParameters(Expression expr, HashSet<string> availableParams, Rule rule)
        {
            switch (expr)
            {
                case ParameterExpression param:
                    if (!availableParams.Contains(param.ParameterName))
                    {
                        AddError($"Rule '{rule.Name}' references undefined parameter '{param.ParameterName}'", rule);
                    }
                    break;

                case BinaryExpression binary:
                    ValidateExpressionParameters(binary.Left, availableParams, rule);
                    ValidateExpressionParameters(binary.Right, availableParams, rule);
                    break;
            }
        }

        #endregion

        #region Rule Conflict Detection

        private void DetectRuleConflicts()
        {
            // Group rules by input symbol
            var rulesBySymbol = _grammar.Rules
                .GroupBy(r => r.InputSymbol.Name)
                .Where(g => g.Count() > 1);

            foreach (var group in rulesBySymbol)
            {
                var rules = group.ToList();
                
                // Check for overlapping or ambiguous rules
                for (int i = 0; i < rules.Count; i++)
                {
                    for (int j = i + 1; j < rules.Count; j++)
                    {
                        var conflict = DetectRuleOverlap(rules[i], rules[j]);
                        if (conflict != null)
                        {
                            AddWarning($"Rules '{rules[i].Name}' and '{rules[j].Name}' may conflict: {conflict}");
                        }
                    }
                }

                // Check for unreachable rules (rules with conditions that can never be true)
                foreach (var rule in rules)
                {
                    if (IsUnreachable(rule, rules))
                    {
                        AddWarning($"Rule '{rule.Name}' may be unreachable");
                    }
                }
            }
        }

        private string DetectRuleOverlap(Rule rule1, Rule rule2)
        {
            // If neither has a condition, they definitely conflict
            if (rule1.Condition == null && rule2.Condition == null)
            {
                return "Both rules match unconditionally";
            }

            // If one has a condition and the other doesn't, potential overlap
            if (rule1.Condition == null || rule2.Condition == null)
            {
                return "One rule matches unconditionally while the other is conditional";
            }

            // Both have conditions - check for logical overlap (simplified analysis)
            // Full SAT solving would be needed for complete analysis
            if (ConditionsCanOverlap(rule1.Condition, rule2.Condition))
            {
                return "Conditions may overlap";
            }

            return null;
        }

        private bool ConditionsCanOverlap(Condition cond1, Condition cond2)
        {
            // Simplified overlap detection
            // In production, this would use more sophisticated analysis
            
            // For now, we assume conditions can overlap unless proven otherwise
            // This is conservative but safe
            return true;
        }

        private bool IsUnreachable(Rule rule, List<Rule> allRules)
        {
            // Check if this rule is always shadowed by earlier rules
            // For now, we use simple heuristics
            
            if (rule.Condition == null)
                return false; // Unconditional rules are always reachable

            // Check if any earlier rule with higher priority always matches when this rule would
            foreach (var other in allRules)
            {
                if (other == rule) continue;
                if (other.Priority > rule.Priority && other.Condition == null)
                {
                    return true; // This rule is shadowed by an unconditional higher-priority rule
                }
            }

            return false;
        }

        #endregion

        #region Choice Validation

        private void ValidateChoiceBlocks()
        {
            foreach (var rule in _grammar.Rules)
            {
                ValidateChoicesInExpansion(rule.Expansion, rule);
            }
        }

        private void ValidateChoicesInExpansion(Expansion expansion, Rule rule)
        {
            switch (expansion)
            {
                case ChooseExpansion choose:
                    if (choose.Options.Count == 0)
                    {
                        AddError($"Rule '{rule.Name}' has a choose block with no options", rule);
                    }

                    // Validate weights
                    float totalWeight = 0;
                    int defaultCount = 0;
                    foreach (var option in choose.Options)
                    {
                        if (option.IsDefault)
                            defaultCount++;
                        
                        if (option.Weight < 0)
                        {
                            AddError($"Rule '{rule.Name}' has a negative weight in choose block", rule);
                        }
                        totalWeight += option.Weight;

                        ValidateChoicesInExpansion(option.Expansion, rule);
                    }

                    if (defaultCount > 1)
                    {
                        AddWarning($"Rule '{rule.Name}' has multiple default options in choose block");
                    }

                    if (totalWeight == 0 && defaultCount == 0)
                    {
                        AddWarning($"Rule '{rule.Name}' has a choose block with zero total weight and no default");
                    }
                    break;

                case SplitExpansion split:
                    foreach (var part in split.Parts)
                        ValidateChoicesInExpansion(part.Expansion, rule);
                    break;

                case RepeatExpansion repeat:
                    ValidateChoicesInExpansion(repeat.Content, rule);
                    break;
            }
        }

        #endregion

        #region Recursion Analysis

        private void AnalyzeRecursion()
        {
            // Build dependency graph
            var graph = BuildSymbolDependencyGraph();

            // Detect cycles (recursion)
            var cycles = DetectCycles(graph);

            foreach (var cycle in cycles)
            {
                // Check if recursion has termination conditions
                if (!HasTerminationCondition(cycle))
                {
                    var cycleStr = string.Join(" -> ", cycle.Select(s => s.Name));
                    AddWarning($"Potentially infinite recursion detected: {cycleStr}");
                }
            }
        }

        private Dictionary<SymbolType, List<SymbolType>> BuildSymbolDependencyGraph()
        {
            var graph = new Dictionary<SymbolType, List<SymbolType>>();

            foreach (var rule in _grammar.Rules)
            {
                if (!graph.ContainsKey(rule.InputSymbol))
                    graph[rule.InputSymbol] = new List<SymbolType>();

                var dependencies = ExtractSymbolDependencies(rule.Expansion);
                graph[rule.InputSymbol].AddRange(dependencies);
            }

            return graph;
        }

        private List<SymbolType> ExtractSymbolDependencies(Expansion expansion)
        {
            var dependencies = new List<SymbolType>();

            switch (expansion)
            {
                case ProductionExpansion prod:
                    dependencies.AddRange(prod.Symbols.Select(s => s.Type));
                    break;

                case SplitExpansion split:
                    foreach (var part in split.Parts)
                        dependencies.AddRange(ExtractSymbolDependencies(part.Expansion));
                    break;

                case RepeatExpansion repeat:
                    dependencies.AddRange(ExtractSymbolDependencies(repeat.Content));
                    break;

                case ChooseExpansion choose:
                    foreach (var option in choose.Options)
                        dependencies.AddRange(ExtractSymbolDependencies(option.Expansion));
                    break;
            }

            return dependencies;
        }

        private List<List<SymbolType>> DetectCycles(Dictionary<SymbolType, List<SymbolType>> graph)
        {
            var cycles = new List<List<SymbolType>>();
            var visited = new HashSet<SymbolType>();
            var recursionStack = new HashSet<SymbolType>();
            var currentPath = new List<SymbolType>();

            foreach (var symbol in graph.Keys)
            {
                if (!visited.Contains(symbol))
                {
                    DetectCyclesDFS(symbol, graph, visited, recursionStack, currentPath, cycles);
                }
            }

            return cycles;
        }

        private void DetectCyclesDFS(
            SymbolType symbol,
            Dictionary<SymbolType, List<SymbolType>> graph,
            HashSet<SymbolType> visited,
            HashSet<SymbolType> recursionStack,
            List<SymbolType> currentPath,
            List<List<SymbolType>> cycles)
        {
            visited.Add(symbol);
            recursionStack.Add(symbol);
            currentPath.Add(symbol);

            if (graph.ContainsKey(symbol))
            {
                foreach (var neighbor in graph[symbol])
                {
                    if (!visited.Contains(neighbor))
                    {
                        DetectCyclesDFS(neighbor, graph, visited, recursionStack, currentPath, cycles);
                    }
                    else if (recursionStack.Contains(neighbor))
                    {
                        // Found a cycle
                        var cycleStart = currentPath.IndexOf(neighbor);
                        var cycle = currentPath.Skip(cycleStart).ToList();
                        cycles.Add(cycle);
                    }
                }
            }

            currentPath.RemoveAt(currentPath.Count - 1);
            recursionStack.Remove(symbol);
        }

        private bool HasTerminationCondition(List<SymbolType> cycle)
        {
            // Check if any rule in the cycle has a condition that could break recursion
            foreach (var symbol in cycle)
            {
                var rules = _grammar.GetRulesFor(symbol);
                if (rules.Any(r => r.Condition != null))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Expression Type Checking

        private void ValidateExpressions()
        {
            foreach (var rule in _grammar.Rules)
            {
                ValidateExpansionExpressions(rule.Expansion, rule);
            }
        }

        private void ValidateExpansionExpressions(Expansion expansion, Rule rule)
        {
            switch (expansion)
            {
                case SplitExpansion split:
                    foreach (var part in split.Parts)
                    {
                        ValidateNumericExpression(part.Size, rule);
                        ValidateExpansionExpressions(part.Expansion, rule);
                    }
                    break;

                case RepeatExpansion repeat:
                    ValidateNumericExpression(repeat.Size, rule);
                    ValidateExpansionExpressions(repeat.Content, rule);
                    break;

                case ChooseExpansion choose:
                    foreach (var option in choose.Options)
                        ValidateExpansionExpressions(option.Expansion, rule);
                    break;
            }
        }

        private void ValidateNumericExpression(Expression expr, Rule rule)
        {
            // Ensure expression evaluates to numeric type
            // This is a simplified check; full type inference would be more comprehensive
            if (expr is LiteralExpression lit)
            {
                if (!(lit.Value is int || lit.Value is float))
                {
                    AddError($"Rule '{rule.Name}' uses non-numeric value where number expected", rule);
                }
            }
        }

        #endregion

        #region Error/Warning Management

        private void AddError(string message, Rule rule = null)
        {
            _errors.Add(new SemanticError
            {
                Message = message,
                RuleName = rule?.Name
            });
        }

        private void AddWarning(string message, Rule rule = null)
        {
            _warnings.Add(new SemanticWarning
            {
                Message = message,
                RuleName = rule?.Name
            });
        }

        #endregion
    }

    #region Result Types

    public class SemanticAnalysisResult
    {
        public bool IsValid { get; set; }
        public List<SemanticError> Errors { get; set; }
        public List<SemanticWarning> Warnings { get; set; }

        public SemanticAnalysisResult()
        {
            Errors = new List<SemanticError>();
            Warnings = new List<SemanticWarning>();
        }

        public override string ToString()
        {
            if (IsValid)
            {
                return Warnings.Count > 0
                    ? $"Valid with {Warnings.Count} warning(s)"
                    : "Valid";
            }
            return $"Invalid: {Errors.Count} error(s), {Warnings.Count} warning(s)";
        }
    }

    public class SemanticError
    {
        public string Message { get; set; }
        public string RuleName { get; set; }

        public override string ToString() =>
            RuleName != null ? $"Error in rule '{RuleName}': {Message}" : $"Error: {Message}";
    }

    public class SemanticWarning
    {
        public string Message { get; set; }
        public string RuleName { get; set; }

        public override string ToString() =>
            RuleName != null ? $"Warning in rule '{RuleName}': {Message}" : $"Warning: {Message}";
    }

    #endregion
}
