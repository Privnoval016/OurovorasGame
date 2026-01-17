using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralGrammarGeneration.GrammarParsing
{
    /// <summary>
    /// Grammar Intermediate Representation (IR).
    /// This is the "compiled bytecode" of the grammar - fully validated, resolved, and optimized.
    /// No strings, all IDs resolved, expressions pre-compiled, rules ordered by priority.
    /// </summary>
    [Serializable]
    public class GrammarIR
    {
        public string Name { get; set; }
        public int Version { get; set; }
        public SymbolType EntrySymbol { get; set; }

        // Lookup tables for fast execution
        public Dictionary<int, SymbolDefinitionIR> Symbols { get; set; }
        public Dictionary<int, List<RuleIR>> RulesBySymbol { get; set; }
        public Dictionary<int, RuleIR> RulesById { get; set; }

        // Metadata for optimization
        public HashSet<int> TerminalSymbols { get; set; }
        public Dictionary<int, int> SymbolDepth { get; set; } // Max derivation depth

        public GrammarIR()
        {
            Symbols = new Dictionary<int, SymbolDefinitionIR>();
            RulesBySymbol = new Dictionary<int, List<RuleIR>>();
            RulesById = new Dictionary<int, RuleIR>();
            TerminalSymbols = new HashSet<int>();
            SymbolDepth = new Dictionary<int, int>();
        }

        public List<RuleIR> GetApplicableRules(SymbolType symbol)
        {
            return RulesBySymbol.TryGetValue(symbol.Id, out var rules) ? rules : new List<RuleIR>();
        }

        public RuleIR GetRule(int ruleId)
        {
            return RulesById.TryGetValue(ruleId, out var rule) ? rule : null;
        }

        public bool IsTerminal(SymbolType symbol)
        {
            return TerminalSymbols.Contains(symbol.Id);
        }
    }

    /// <summary>
    /// Compiled symbol definition with resolved types.
    /// </summary>
    [Serializable]
    public class SymbolDefinitionIR
    {
        public SymbolType Type { get; set; }
        public List<ParameterDefinitionIR> Parameters { get; set; }
        public bool IsTerminal { get; set; }

        public SymbolDefinitionIR()
        {
            Parameters = new List<ParameterDefinitionIR>();
        }
    }

    /// <summary>
    /// Compiled parameter definition.
    /// </summary>
    [Serializable]
    public class ParameterDefinitionIR
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ParameterType Type { get; set; }
        public bool IsRequired { get; set; }
        public object DefaultValue { get; set; }
    }

    /// <summary>
    /// Compiled rule with pre-computed priorities and resolved references.
    /// </summary>
    [Serializable]
    public class RuleIR
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SymbolType InputSymbol { get; set; }
        public List<ParameterDefinitionIR> Parameters { get; set; }
        public ConditionIR Condition { get; set; }
        public ExpansionIR Expansion { get; set; }
        public int Priority { get; set; }
        public int EstimatedCost { get; set; } // For optimization

        public RuleIR()
        {
            Parameters = new List<ParameterDefinitionIR>();
        }
    }

    /// <summary>
    /// Compiled condition with optimized evaluation.
    /// </summary>
    [Serializable]
    public abstract class ConditionIR
    {
        public abstract bool Evaluate(Dictionary<string, object> context);
    }

    [Serializable]
    public class ComparisonConditionIR : ConditionIR
    {
        public int ParameterId { get; set; }
        public string ParameterName { get; set; }
        public ComparisonOperator Operator { get; set; }
        public object RightValue { get; set; }

        public override bool Evaluate(Dictionary<string, object> context)
        {
            if (!context.TryGetValue(ParameterName, out var leftValue))
                return false;

            return Operator switch
            {
                ComparisonOperator.Equal => Equals(leftValue, RightValue),
                ComparisonOperator.NotEqual => !Equals(leftValue, RightValue),
                ComparisonOperator.LessThan => Compare(leftValue, RightValue) < 0,
                ComparisonOperator.LessOrEqual => Compare(leftValue, RightValue) <= 0,
                ComparisonOperator.GreaterThan => Compare(leftValue, RightValue) > 0,
                ComparisonOperator.GreaterOrEqual => Compare(leftValue, RightValue) >= 0,
                _ => false
            };
        }

        private int Compare(object left, object right)
        {
            if (left is IComparable leftComp && right is IComparable rightComp)
                return leftComp.CompareTo(rightComp);
            return 0;
        }
    }

    [Serializable]
    public class LogicalConditionIR : ConditionIR
    {
        public LogicalOperator Operator { get; set; }
        public List<ConditionIR> Operands { get; set; }

        public LogicalConditionIR()
        {
            Operands = new List<ConditionIR>();
        }

        public override bool Evaluate(Dictionary<string, object> context)
        {
            return Operator switch
            {
                LogicalOperator.And => Operands.All(c => c.Evaluate(context)),
                LogicalOperator.Or => Operands.Any(c => c.Evaluate(context)),
                LogicalOperator.Not => Operands.Count > 0 && !Operands[0].Evaluate(context),
                _ => false
            };
        }
    }

    /// <summary>
    /// Base class for compiled expansions.
    /// </summary>
    [Serializable]
    public abstract class ExpansionIR
    {
    }

    [Serializable]
    public class ProductionExpansionIR : ExpansionIR
    {
        public List<SymbolInstanceIR> Symbols { get; set; }

        public ProductionExpansionIR()
        {
            Symbols = new List<SymbolInstanceIR>();
        }
    }

    [Serializable]
    public class SymbolInstanceIR
    {
        public SymbolType Type { get; set; }
        public Dictionary<int, ExpressionIR> ParameterExpressions { get; set; } // ParamId -> Expression
        public Dictionary<int, string> ParameterNames { get; set; } // ParamId -> Name (for lookup)

        public SymbolInstanceIR()
        {
            ParameterExpressions = new Dictionary<int, ExpressionIR>();
            ParameterNames = new Dictionary<int, string>();
        }
    }

    [Serializable]
    public class SplitExpansionIR : ExpansionIR
    {
        public SplitAxis Axis { get; set; }
        public List<SplitPartIR> Parts { get; set; }

        public SplitExpansionIR()
        {
            Parts = new List<SplitPartIR>();
        }
    }

    [Serializable]
    public class SplitPartIR
    {
        public SizeMode Mode { get; set; }
        public ExpressionIR Size { get; set; }
        public ExpansionIR Expansion { get; set; }
    }

    [Serializable]
    public class RepeatExpansionIR : ExpansionIR
    {
        public SplitAxis Axis { get; set; }
        public ExpressionIR Size { get; set; }
        public ExpansionIR Content { get; set; }
    }

    [Serializable]
    public class ChooseExpansionIR : ExpansionIR
    {
        public List<ChoiceOptionIR> Options { get; set; }
        public int DefaultOptionIndex { get; set; }
        public float TotalWeight { get; set; } // Pre-computed for performance

        public ChooseExpansionIR()
        {
            Options = new List<ChoiceOptionIR>();
            DefaultOptionIndex = -1;
        }
    }

    [Serializable]
    public class ChoiceOptionIR
    {
        public int Index { get; set; }
        public bool IsDefault { get; set; }
        public float Weight { get; set; }
        public float CumulativeWeight { get; set; } // For weighted random selection
        public ExpansionIR Expansion { get; set; }
    }

    /// <summary>
    /// Compiled expression with optimized evaluation.
    /// </summary>
    [Serializable]
    public abstract class ExpressionIR
    {
        public abstract object Evaluate(Dictionary<string, object> context);
    }

    [Serializable]
    public class LiteralExpressionIR : ExpressionIR
    {
        public object Value { get; set; }

        public override object Evaluate(Dictionary<string, object> context) => Value;
    }

    [Serializable]
    public class ParameterExpressionIR : ExpressionIR
    {
        public int ParameterId { get; set; }
        public string ParameterName { get; set; }

        public override object Evaluate(Dictionary<string, object> context)
        {
            return context.TryGetValue(ParameterName, out var value) ? value : null;
        }
    }

    [Serializable]
    public class BinaryExpressionIR : ExpressionIR
    {
        public ExpressionIR Left { get; set; }
        public BinaryOperator Operator { get; set; }
        public ExpressionIR Right { get; set; }

        public override object Evaluate(Dictionary<string, object> context)
        {
            var left = Left.Evaluate(context);
            var right = Right.Evaluate(context);

            if (left == null || right == null) return null;

            return Operator switch
            {
                BinaryOperator.Add => Add(left, right),
                BinaryOperator.Subtract => Subtract(left, right),
                BinaryOperator.Multiply => Multiply(left, right),
                BinaryOperator.Divide => Divide(left, right),
                _ => null
            };
        }

        private object Add(object left, object right)
        {
            return (left, right) switch
            {
                (float lf, float rf) => lf + rf,
                (int li, int ri) => li + ri,
                (float lf, int ri) => lf + ri,
                (int li, float rf) => li + rf,
                _ => null
            };
        }

        private object Subtract(object left, object right)
        {
            return (left, right) switch
            {
                (float lf, float rf) => lf - rf,
                (int li, int ri) => li - ri,
                (float lf, int ri) => lf - ri,
                (int li, float rf) => li - rf,
                _ => null
            };
        }

        private object Multiply(object left, object right)
        {
            return (left, right) switch
            {
                (float lf, float rf) => lf * rf,
                (int li, int ri) => li * ri,
                (float lf, int ri) => lf * ri,
                (int li, float rf) => li * rf,
                _ => null
            };
        }

        private object Divide(object left, object right)
        {
            return (left, right) switch
            {
                (float lf, float rf) when rf != 0 => lf / rf,
                (int li, int ri) when ri != 0 => li / ri,
                (float lf, int ri) when ri != 0 => lf / ri,
                (int li, float rf) when rf != 0 => li / rf,
                _ => null
            };
        }
    }

    /// <summary>
    /// Compiles a validated GrammarDefinition into optimized Grammar IR.
    /// </summary>
    public class GrammarCompiler
    {
        private GrammarDefinition _grammar;
        private Dictionary<string, int> _parameterIds;
        private int _nextParamId;

        public GrammarCompiler(GrammarDefinition grammar)
        {
            _grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            _parameterIds = new Dictionary<string, int>();
            _nextParamId = 0;
        }

        public GrammarIR Compile()
        {
            var ir = new GrammarIR
            {
                Name = _grammar.Name,
                Version = 1,
                EntrySymbol = _grammar.EntrySymbol
            };

            // Compile symbols
            foreach (var symbolDef in _grammar.Symbols)
            {
                var symbolIR = CompileSymbol(symbolDef);
                ir.Symbols[symbolDef.Type.Id] = symbolIR;

                if (symbolDef.IsTerminal)
                    ir.TerminalSymbols.Add(symbolDef.Type.Id);
            }

            // Compile rules
            foreach (var rule in _grammar.Rules)
            {
                var ruleIR = CompileRule(rule);
                ir.RulesById[ruleIR.Id] = ruleIR;

                if (!ir.RulesBySymbol.ContainsKey(rule.InputSymbol.Id))
                    ir.RulesBySymbol[rule.InputSymbol.Id] = new List<RuleIR>();

                ir.RulesBySymbol[rule.InputSymbol.Id].Add(ruleIR);
            }

            // Sort rules by priority within each symbol group
            foreach (var rules in ir.RulesBySymbol.Values)
            {
                rules.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }

            // Compute symbol depths for optimization
            ComputeSymbolDepths(ir);

            return ir;
        }

        private SymbolDefinitionIR CompileSymbol(SymbolDefinition symbolDef)
        {
            return new SymbolDefinitionIR
            {
                Type = symbolDef.Type,
                IsTerminal = symbolDef.IsTerminal,
                Parameters = symbolDef.Parameters.Select(CompileParameter).ToList()
            };
        }

        private ParameterDefinitionIR CompileParameter(ParameterDefinition paramDef)
        {
            var id = GetOrCreateParameterId(paramDef.Name);
            return new ParameterDefinitionIR
            {
                Id = id,
                Name = paramDef.Name,
                Type = paramDef.Type,
                IsRequired = paramDef.IsRequired,
                DefaultValue = paramDef.DefaultValue
            };
        }

        private RuleIR CompileRule(Rule rule)
        {
            _parameterIds.Clear(); // Reset for each rule

            return new RuleIR
            {
                Id = rule.Id,
                Name = rule.Name,
                InputSymbol = rule.InputSymbol,
                Parameters = rule.Parameters.Select(CompileParameter).ToList(),
                Condition = rule.Condition != null ? CompileCondition(rule.Condition) : null,
                Expansion = CompileExpansion(rule.Expansion),
                Priority = rule.Priority,
                EstimatedCost = EstimateRuleCost(rule)
            };
        }

        private ConditionIR CompileCondition(Condition condition)
        {
            return condition switch
            {
                ComparisonCondition comp => new ComparisonConditionIR
                {
                    ParameterId = GetOrCreateParameterId(comp.LeftOperand),
                    ParameterName = comp.LeftOperand,
                    Operator = comp.Operator,
                    RightValue = comp.RightOperand
                },
                LogicalCondition logical => new LogicalConditionIR
                {
                    Operator = logical.Operator,
                    Operands = logical.Operands.Select(CompileCondition).ToList()
                },
                _ => throw new NotSupportedException($"Unknown condition type: {condition.GetType()}")
            };
        }

        private ExpansionIR CompileExpansion(Expansion expansion)
        {
            return expansion switch
            {
                ProductionExpansion prod => new ProductionExpansionIR
                {
                    Symbols = prod.Symbols.Select(CompileSymbolInstance).ToList()
                },
                SplitExpansion split => new SplitExpansionIR
                {
                    Axis = split.Axis,
                    Parts = split.Parts.Select(CompileSplitPart).ToList()
                },
                RepeatExpansion repeat => new RepeatExpansionIR
                {
                    Axis = repeat.Axis,
                    Size = CompileExpression(repeat.Size),
                    Content = CompileExpansion(repeat.Content)
                },
                ChooseExpansion choose => CompileChooseExpansion(choose),
                _ => throw new NotSupportedException($"Unknown expansion type: {expansion.GetType()}")
            };
        }

        private SymbolInstanceIR CompileSymbolInstance(SymbolInstance instance)
        {
            var ir = new SymbolInstanceIR { Type = instance.Type };

            foreach (var kvp in instance.ParameterExpressions)
            {
                var paramId = GetOrCreateParameterId(kvp.Key);
                ir.ParameterExpressions[paramId] = CompileExpression(kvp.Value);
                ir.ParameterNames[paramId] = kvp.Key; // Store the parameter name
            }

            return ir;
        }

        private SplitPartIR CompileSplitPart(SplitPart part)
        {
            return new SplitPartIR
            {
                Mode = part.Mode,
                Size = CompileExpression(part.Size),
                Expansion = CompileExpansion(part.Expansion)
            };
        }

        private ChooseExpansionIR CompileChooseExpansion(ChooseExpansion choose)
        {
            var ir = new ChooseExpansionIR
            {
                DefaultOptionIndex = choose.DefaultOptionIndex
            };

            float cumulativeWeight = 0;
            for (int i = 0; i < choose.Options.Count; i++)
            {
                var option = choose.Options[i];
                cumulativeWeight += option.Weight;

                ir.Options.Add(new ChoiceOptionIR
                {
                    Index = i,
                    IsDefault = option.IsDefault,
                    Weight = option.Weight,
                    CumulativeWeight = cumulativeWeight,
                    Expansion = CompileExpansion(option.Expansion)
                });
            }

            ir.TotalWeight = cumulativeWeight;
            return ir;
        }

        private ExpressionIR CompileExpression(Expression expr)
        {
            return expr switch
            {
                LiteralExpression lit => new LiteralExpressionIR { Value = lit.Value },
                ParameterExpression param => new ParameterExpressionIR
                {
                    ParameterId = GetOrCreateParameterId(param.ParameterName),
                    ParameterName = param.ParameterName
                },
                BinaryExpression binary => new BinaryExpressionIR
                {
                    Left = CompileExpression(binary.Left),
                    Operator = binary.Operator,
                    Right = CompileExpression(binary.Right)
                },
                _ => throw new NotSupportedException($"Unknown expression type: {expr.GetType()}")
            };
        }

        private int GetOrCreateParameterId(string name)
        {
            if (!_parameterIds.ContainsKey(name))
                _parameterIds[name] = _nextParamId++;
            return _parameterIds[name];
        }

        private int EstimateRuleCost(Rule rule)
        {
            // Simple heuristic: cost increases with expansion complexity
            return EstimateExpansionCost(rule.Expansion);
        }

        private int EstimateExpansionCost(Expansion expansion)
        {
            return expansion switch
            {
                ProductionExpansion prod => prod.Symbols.Count,
                SplitExpansion split => split.Parts.Sum(p => EstimateExpansionCost(p.Expansion)) + 10,
                RepeatExpansion repeat => EstimateExpansionCost(repeat.Content) * 10,
                ChooseExpansion choose => choose.Options.Max(o => EstimateExpansionCost(o.Expansion)),
                _ => 1
            };
        }

        private void ComputeSymbolDepths(GrammarIR ir)
        {
            // Compute maximum derivation depth for each symbol
            // This is useful for optimization and recursion limiting
            
            var computed = new HashSet<int>();
            foreach (var symbolId in ir.Symbols.Keys)
            {
                ComputeSymbolDepth(ir, symbolId, computed, new HashSet<int>());
            }
        }

        private int ComputeSymbolDepth(GrammarIR ir, int symbolId, HashSet<int> computed, HashSet<int> visiting)
        {
            if (computed.Contains(symbolId))
                return ir.SymbolDepth[symbolId];

            if (visiting.Contains(symbolId))
            {
                // Recursion detected
                ir.SymbolDepth[symbolId] = int.MaxValue;
                return int.MaxValue;
            }

            visiting.Add(symbolId);

            var rules = ir.RulesBySymbol.ContainsKey(symbolId) ? ir.RulesBySymbol[symbolId] : new List<RuleIR>();
            var maxDepth = 0;

            foreach (var rule in rules)
            {
                var expansionDepth = ComputeExpansionDepth(ir, rule.Expansion, computed, visiting);
                maxDepth = Math.Max(maxDepth, expansionDepth);
            }

            visiting.Remove(symbolId);
            computed.Add(symbolId);
            ir.SymbolDepth[symbolId] = maxDepth + 1;
            return maxDepth + 1;
        }

        private int ComputeExpansionDepth(GrammarIR ir, ExpansionIR expansion, HashSet<int> computed, HashSet<int> visiting)
        {
            return expansion switch
            {
                ProductionExpansionIR prod => prod.Symbols.Max(s => ComputeSymbolDepth(ir, s.Type.Id, computed, visiting)),
                SplitExpansionIR split => split.Parts.Max(p => ComputeExpansionDepth(ir, p.Expansion, computed, visiting)),
                RepeatExpansionIR repeat => ComputeExpansionDepth(ir, repeat.Content, computed, visiting),
                ChooseExpansionIR choose => choose.Options.Max(o => ComputeExpansionDepth(ir, o.Expansion, computed, visiting)),
                _ => 0
            };
        }
    }
}
