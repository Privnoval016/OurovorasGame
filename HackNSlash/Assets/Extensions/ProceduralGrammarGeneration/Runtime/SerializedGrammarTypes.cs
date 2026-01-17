using System;
using System.Collections.Generic;
using ProceduralGrammarGeneration.GrammarParsing;
using UnityEngine;

namespace ProceduralGrammarGeneration.Runtime
{
    /// <summary>
    /// Serializable wrapper for grammar data that can be saved in Unity assets.
    /// These types mirror the backend grammar system but are Unity-serializable.
    /// </summary>
    /// 
    [Serializable]
    public class SerializedSymbol
    {
        public string name = "Symbol";
        [TextArea(2, 4)]
        public string description = ""; // User-facing comment/documentation
        public List<SerializedParameter> parameters = new List<SerializedParameter>();
        
        public SerializedSymbol() { }
        
        public SerializedSymbol(string name)
        {
            this.name = name;
        }
    }

    [Serializable]
    public class SerializedParameter
    {
        public string name = "param";
        public ParameterType type = ParameterType.Float;
        public string defaultValue = "0";
        public string description = ""; // Optional comment for this parameter
        
        // For spatial parameters
        public UnityEngine.Object spatialDataReference; // Spline, Terrain, GameObject, etc.
        
        public SerializedParameter() { }
        
        public SerializedParameter(string name, ParameterType type, string defaultValue = "")
        {
            this.name = name;
            this.type = type;
            this.defaultValue = defaultValue;
        }
    }

    [Serializable]
    public class SerializedRule
    {
        public string name = "Rule";
        [TextArea(2, 4)]
        public string description = ""; // User-facing comment/documentation
        public SerializedSymbol predecessor = new SerializedSymbol();
        public List<SerializedProduction> productions = new List<SerializedProduction>();
        
        public SerializedRule() { }
        
        public SerializedRule(string name)
        {
            this.name = name;
        }
    }

    [Serializable]
    public class SerializedProduction
    {
        public float weight = 1f;
        public List<SerializedProductionStep> steps = new List<SerializedProductionStep>();
        public List<SerializedCondition> conditions = new List<SerializedCondition>();
        
        public SerializedProduction()
        {
            steps.Add(new SerializedProductionStep());
        }
    }

    [Serializable]
    public class SerializedProductionStep
    {
        public ProductionStepType type = ProductionStepType.Symbol;
        
        // For Symbol type
        public string symbolName = "";
        public List<SerializedParameterAssignment> parameterAssignments = new List<SerializedParameterAssignment>();
        
        // For operation types
        public string operationTarget = ""; // parameter name
        public string operationValue = "";  // expression or value
        public OperationType operationType = OperationType.Set;
        
        public SerializedProductionStep() { }
    }

    [Serializable]
    public enum ProductionStepType
    {
        Symbol,
        SetParameter,
        ModifyParameter,
        ConditionalBranch
    }

    [Serializable]
    public enum OperationType
    {
        Set,
        Add,
        Multiply,
        Subtract,
        Divide
    }

    [Serializable]
    public class SerializedParameterAssignment
    {
        public string parameterName = "";
        public string valueExpression = ""; // Can be literal value, parameter reference, or expression
        
        public SerializedParameterAssignment() { }
        
        public SerializedParameterAssignment(string name, string value)
        {
            parameterName = name;
            valueExpression = value;
        }
    }

    [Serializable]
    public class SerializedCondition
    {
        public string leftOperand = "";  // parameter name or expression
        public ConditionOperator op = ConditionOperator.GreaterThan;
        public string rightOperand = ""; // value or parameter name
        
        public SerializedCondition() { }
    }

    [Serializable]
    public enum ConditionOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual
    }
}
