using System;
using System.Collections.Generic;

namespace ProceduralGrammarGeneration.GrammarParsing
{
    /// <summary>
    /// Core data types and structures for the grammar system.
    /// This file defines the fundamental building blocks used throughout the parser.
    /// </summary>

    #region Symbol Definitions

    /// <summary>
    /// Represents a unique symbol type in the grammar (e.g., "Facade", "Window", "Branch").
    /// Symbols are typed, parameterized semantic instructions, not geometry.
    /// </summary>
    [Serializable]
    public struct SymbolType : IEquatable<SymbolType>
    {
        public string Name { get; }
        public int Id { get; }

        public SymbolType(string name, int id)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Id = id;
        }

        public bool Equals(SymbolType other) => Id == other.Id;
        public override bool Equals(object obj) => obj is SymbolType other && Equals(other);
        public override int GetHashCode() => Id;
        public override string ToString() => Name;

        public static bool operator ==(SymbolType left, SymbolType right) => left.Equals(right);
        public static bool operator !=(SymbolType left, SymbolType right) => !left.Equals(right);
    }

    /// <summary>
    /// Represents a parameter definition for a symbol or rule.
    /// </summary>
    [Serializable]
    public class ParameterDefinition
    {
        public string Name { get; set; }
        public ParameterType Type { get; set; }
        public bool IsRequired { get; set; }
        public object DefaultValue { get; set; }

        public ParameterDefinition(string name, ParameterType type, bool isRequired = true, object defaultValue = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
            IsRequired = isRequired;
            DefaultValue = defaultValue;
        }
    }

    /// <summary>
    /// Supported parameter types in the grammar system.
    /// </summary>
    public enum ParameterType
    {
        Float,
        Int,
        String,
        Bool,
        Vector3,
        Color,
        // Spatial types for procedural path control
        SplineCurve,
        Heightmap,
        PointField,
        SpatialConstant
    }

    /// <summary>
    /// A symbol instance in the derivation tree with concrete parameter values.
    /// </summary>
    [Serializable]
    public class Symbol
    {
        public SymbolType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public ScopeId Scope { get; set; }
        public int RuleId { get; set; } // The rule that produced this symbol

        public Symbol(SymbolType type, Dictionary<string, object> parameters = null)
        {
            Type = type;
            Parameters = parameters ?? new Dictionary<string, object>();
        }

        public T GetParameter<T>(string name)
        {
            if (Parameters.TryGetValue(name, out var value) && value is T typedValue)
                return typedValue;
            throw new ArgumentException($"Parameter '{name}' not found or wrong type");
        }

        public bool TryGetParameter<T>(string name, out T value)
        {
            if (Parameters.TryGetValue(name, out var rawValue) && rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Gets a spatial data parameter (SplineCurve, Heightmap, PointField, etc.).
        /// </summary>
        public TSpatial GetSpatialParameter<TSpatial>(string name) where TSpatial : ISpatialData
        {
            if (Parameters.TryGetValue(name, out var value) && value is TSpatial spatialValue)
                return spatialValue;
            throw new ArgumentException($"Spatial parameter '{name}' not found or wrong type");
        }

        /// <summary>
        /// Tries to get a spatial data parameter.
        /// </summary>
        public bool TryGetSpatialParameter<TSpatial>(string name, out TSpatial value) where TSpatial : ISpatialData
        {
            if (Parameters.TryGetValue(name, out var rawValue) && rawValue is TSpatial spatialValue)
            {
                value = spatialValue;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Sets a spatial data parameter.
        /// </summary>
        public void SetSpatialParameter(string name, ISpatialData spatialData)
        {
            Parameters[name] = spatialData ?? throw new ArgumentNullException(nameof(spatialData));
        }
    }

    #endregion

    #region Scope System

    /// <summary>
    /// Unique identifier for a scope in the derivation tree.
    /// Enables partial regeneration, override tracking, and deterministic seeding.
    /// </summary>
    [Serializable]
    public struct ScopeId : IEquatable<ScopeId>
    {
        public string Path { get; }
        public int Hash { get; }

        public ScopeId(string path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Hash = path.GetHashCode();
        }

        public ScopeId(ScopeId parent, string childName, int childIndex)
        {
            Path = $"{parent.Path}/{childName}[{childIndex}]";
            Hash = Path.GetHashCode();
        }

        public static ScopeId Root => new ScopeId("Root");

        public bool Equals(ScopeId other) => Hash == other.Hash;
        public override bool Equals(object obj) => obj is ScopeId other && Equals(other);
        public override int GetHashCode() => Hash;
        public override string ToString() => Path;

        public static bool operator ==(ScopeId left, ScopeId right) => left.Equals(right);
        public static bool operator !=(ScopeId left, ScopeId right) => !left.Equals(right);
    }

    #endregion

    #region Rule Definitions

    /// <summary>
    /// Defines a production rule in the grammar.
    /// Format: rule Name when Symbol(params...) if Condition => Expansion
    /// </summary>
    [Serializable]
    public class Rule
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SymbolType InputSymbol { get; set; }
        public List<ParameterDefinition> Parameters { get; set; }
        public Condition Condition { get; set; } // Optional
        public Expansion Expansion { get; set; }
        public int Priority { get; set; } // For conflict resolution

        public Rule()
        {
            Parameters = new List<ParameterDefinition>();
        }

        public override string ToString()
        {
            var condStr = Condition != null ? $" if {Condition}" : "";
            return $"rule {Name} when {InputSymbol}({string.Join(", ", Parameters)}){condStr} => {Expansion}";
        }
    }

    /// <summary>
    /// Base class for conditional expressions.
    /// </summary>
    [Serializable]
    public abstract class Condition
    {
        public abstract bool Evaluate(Dictionary<string, object> context);
    }

    /// <summary>
    /// Binary comparison condition (e.g., floorIndex == 0, width > 10).
    /// </summary>
    [Serializable]
    public class ComparisonCondition : Condition
    {
        public string LeftOperand { get; set; }
        public ComparisonOperator Operator { get; set; }
        public object RightOperand { get; set; }

        public override bool Evaluate(Dictionary<string, object> context)
        {
            if (!context.TryGetValue(LeftOperand, out var leftValue))
                throw new InvalidOperationException($"Parameter '{LeftOperand}' not found in context");

            return Operator switch
            {
                ComparisonOperator.Equal => Equals(leftValue, RightOperand),
                ComparisonOperator.NotEqual => !Equals(leftValue, RightOperand),
                ComparisonOperator.LessThan => Compare(leftValue, RightOperand) < 0,
                ComparisonOperator.LessOrEqual => Compare(leftValue, RightOperand) <= 0,
                ComparisonOperator.GreaterThan => Compare(leftValue, RightOperand) > 0,
                ComparisonOperator.GreaterOrEqual => Compare(leftValue, RightOperand) >= 0,
                _ => throw new NotImplementedException($"Operator {Operator} not implemented")
            };
        }

        private int Compare(object left, object right)
        {
            if (left is IComparable leftComp && right is IComparable rightComp)
                return leftComp.CompareTo(rightComp);
            throw new InvalidOperationException($"Cannot compare {left?.GetType()} with {right?.GetType()}");
        }

        public override string ToString() => $"{LeftOperand} {OperatorToString(Operator)} {RightOperand}";

        private string OperatorToString(ComparisonOperator op) => op switch
        {
            ComparisonOperator.Equal => "==",
            ComparisonOperator.NotEqual => "!=",
            ComparisonOperator.LessThan => "<",
            ComparisonOperator.LessOrEqual => "<=",
            ComparisonOperator.GreaterThan => ">",
            ComparisonOperator.GreaterOrEqual => ">=",
            _ => op.ToString()
        };
    }

    /// <summary>
    /// Logical combination of conditions (AND, OR, NOT).
    /// </summary>
    [Serializable]
    public class LogicalCondition : Condition
    {
        public LogicalOperator Operator { get; set; }
        public List<Condition> Operands { get; set; }

        public LogicalCondition()
        {
            Operands = new List<Condition>();
        }

        public override bool Evaluate(Dictionary<string, object> context)
        {
            return Operator switch
            {
                LogicalOperator.And => Operands.TrueForAll(c => c.Evaluate(context)),
                LogicalOperator.Or => Operands.Exists(c => c.Evaluate(context)),
                LogicalOperator.Not => Operands.Count == 1 && !Operands[0].Evaluate(context),
                _ => throw new NotImplementedException($"Operator {Operator} not implemented")
            };
        }

        public override string ToString()
        {
            var op = Operator == LogicalOperator.And ? "&&" : "||";
            return Operator == LogicalOperator.Not
                ? $"!({Operands[0]})"
                : $"({string.Join($" {op} ", Operands)})";
        }
    }

    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        LessThan,
        LessOrEqual,
        GreaterThan,
        GreaterOrEqual
    }

    public enum LogicalOperator
    {
        And,
        Or,
        Not
    }

    #endregion

    #region Expansion Types

    /// <summary>
    /// Base class for all expansion types (production, split, repeat, choose).
    /// </summary>
    [Serializable]
    public abstract class Expansion
    {
    }

    /// <summary>
    /// Simple production: Symbol → Symbol(s).
    /// </summary>
    [Serializable]
    public class ProductionExpansion : Expansion
    {
        public List<SymbolInstance> Symbols { get; set; }

        public ProductionExpansion()
        {
            Symbols = new List<SymbolInstance>();
        }

        public override string ToString() => string.Join(", ", Symbols);
    }

    /// <summary>
    /// A symbol instance with parameter expressions.
    /// </summary>
    [Serializable]
    public class SymbolInstance
    {
        public SymbolType Type { get; set; }
        public Dictionary<string, Expression> ParameterExpressions { get; set; }

        public SymbolInstance(SymbolType type)
        {
            Type = type;
            ParameterExpressions = new Dictionary<string, Expression>();
        }

        public override string ToString()
        {
            var args = string.Join(", ", ParameterExpressions);
            return $"{Type}({args})";
        }
    }

    /// <summary>
    /// Split expansion: divide space along an axis.
    /// </summary>
    [Serializable]
    public class SplitExpansion : Expansion
    {
        public SplitAxis Axis { get; set; }
        public List<SplitPart> Parts { get; set; }

        public SplitExpansion()
        {
            Parts = new List<SplitPart>();
        }

        public override string ToString() => $"split({Axis}) {{ {string.Join(", ", Parts)} }}";
    }

    [Serializable]
    public class SplitPart
    {
        public SizeMode Mode { get; set; }
        public Expression Size { get; set; }
        public Expansion Expansion { get; set; }

        public override string ToString()
        {
            var sizeStr = Mode == SizeMode.Relative ? $"{Size}r" : Size.ToString();
            return $"{sizeStr}: {Expansion}";
        }
    }

    public enum SplitAxis
    {
        X,
        Y,
        Z
    }

    public enum SizeMode
    {
        Absolute,
        Relative,
        Repeat
    }

    /// <summary>
    /// Repeat expansion: tile a symbol along an axis.
    /// </summary>
    [Serializable]
    public class RepeatExpansion : Expansion
    {
        public SplitAxis Axis { get; set; }
        public Expression Size { get; set; }
        public Expansion Content { get; set; }

        public override string ToString() => $"repeat({Axis}, {Size}) {{ {Content} }}";
    }

    /// <summary>
    /// Choose expansion: multiple alternatives with selection policy.
    /// </summary>
    [Serializable]
    public class ChooseExpansion : Expansion
    {
        public List<ChoiceOption> Options { get; set; }
        public int DefaultOptionIndex { get; set; } // -1 if not specified

        public ChooseExpansion()
        {
            Options = new List<ChoiceOption>();
            DefaultOptionIndex = -1;
        }

        public override string ToString() => $"choose {{ {string.Join(", ", Options)} }}";
    }

    [Serializable]
    public class ChoiceOption
    {
        public bool IsDefault { get; set; }
        public float Weight { get; set; } // For weighted random selection
        public Expansion Expansion { get; set; }

        public override string ToString()
        {
            var prefix = IsDefault ? "default" : Weight > 0 ? $"weight {Weight}" : "option";
            return $"{prefix}: {Expansion}";
        }
    }

    #endregion

    #region Expression System

    /// <summary>
    /// Base class for parameter expressions.
    /// </summary>
    [Serializable]
    public abstract class Expression
    {
        public abstract object Evaluate(Dictionary<string, object> context);
    }

    /// <summary>
    /// Literal constant value.
    /// </summary>
    [Serializable]
    public class LiteralExpression : Expression
    {
        public object Value { get; set; }

        public LiteralExpression(object value)
        {
            Value = value;
        }

        public override object Evaluate(Dictionary<string, object> context) => Value;
        public override string ToString() => Value?.ToString() ?? "null";
    }

    /// <summary>
    /// Parameter reference (e.g., "width", "floorIndex").
    /// </summary>
    [Serializable]
    public class ParameterExpression : Expression
    {
        public string ParameterName { get; set; }

        public ParameterExpression(string parameterName)
        {
            ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
        }

        public override object Evaluate(Dictionary<string, object> context)
        {
            if (context.TryGetValue(ParameterName, out var value))
                return value;
            throw new InvalidOperationException($"Parameter '{ParameterName}' not found in context");
        }

        public override string ToString() => ParameterName;
    }

    /// <summary>
    /// Binary arithmetic expression (e.g., width * 0.5, height + 10).
    /// </summary>
    [Serializable]
    public class BinaryExpression : Expression
    {
        public Expression Left { get; set; }
        public BinaryOperator Operator { get; set; }
        public Expression Right { get; set; }

        public override object Evaluate(Dictionary<string, object> context)
        {
            var left = Left.Evaluate(context);
            var right = Right.Evaluate(context);

            return Operator switch
            {
                BinaryOperator.Add => Add(left, right),
                BinaryOperator.Subtract => Subtract(left, right),
                BinaryOperator.Multiply => Multiply(left, right),
                BinaryOperator.Divide => Divide(left, right),
                _ => throw new NotImplementedException($"Operator {Operator} not implemented")
            };
        }

        private object Add(object left, object right)
        {
            if (left is float lf && right is float rf) return lf + rf;
            if (left is int li && right is int ri) return li + ri;
            if (left is float lf2 && right is int ri2) return lf2 + ri2;
            if (left is int li2 && right is float rf2) return li2 + rf2;
            throw new InvalidOperationException($"Cannot add {left?.GetType()} and {right?.GetType()}");
        }

        private object Subtract(object left, object right)
        {
            if (left is float lf && right is float rf) return lf - rf;
            if (left is int li && right is int ri) return li - ri;
            if (left is float lf2 && right is int ri2) return lf2 - ri2;
            if (left is int li2 && right is float rf2) return li2 - rf2;
            throw new InvalidOperationException($"Cannot subtract {left?.GetType()} and {right?.GetType()}");
        }

        private object Multiply(object left, object right)
        {
            if (left is float lf && right is float rf) return lf * rf;
            if (left is int li && right is int ri) return li * ri;
            if (left is float lf2 && right is int ri2) return lf2 * ri2;
            if (left is int li2 && right is float rf2) return li2 * rf2;
            throw new InvalidOperationException($"Cannot multiply {left?.GetType()} and {right?.GetType()}");
        }

        private object Divide(object left, object right)
        {
            if (left is float lf && right is float rf) return lf / rf;
            if (left is int li && right is int ri) return li / ri;
            if (left is float lf2 && right is int ri2) return lf2 / ri2;
            if (left is int li2 && right is float rf2) return li2 / rf2;
            throw new InvalidOperationException($"Cannot divide {left?.GetType()} and {right?.GetType()}");
        }

        public override string ToString()
        {
            var op = Operator switch
            {
                BinaryOperator.Add => "+",
                BinaryOperator.Subtract => "-",
                BinaryOperator.Multiply => "*",
                BinaryOperator.Divide => "/",
                _ => Operator.ToString()
            };
            return $"({Left} {op} {Right})";
        }
    }

    public enum BinaryOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    #endregion
}
