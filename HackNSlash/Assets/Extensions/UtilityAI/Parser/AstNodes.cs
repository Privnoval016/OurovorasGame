using System;
using System.Collections.Generic;

namespace Extensions.UtilityAI.Parser
{
    // ── Token ────────────────────────────────────────────────────────────────────

    /** <summary>All lexical token kinds in the AI definition language.</summary> */
    public enum TokenKind
    {
        // Structural
        Identifier,     // e.g. action, consideration, composite, curve
        String,         // "quoted text"
        Number,         // 3.14
        Bool,           // true | false
        LeftBrace,      // {
        RightBrace,     // }
        LeftParen,      // (
        RightParen,     // )
        LeftBracket,    // [
        RightBracket,   // ]
        Colon,          // :
        Comma,          // ,
        Equals,         // =
        At,             // @ (annotation prefix)
        Newline,
        EOF,
    }

    /** <summary>One token produced by the lexer.</summary> */
    public readonly struct Token
    {
        public readonly TokenKind Kind;
        public readonly string Value;
        public readonly int Line;

        public Token(TokenKind kind, string value, int line) { Kind = kind; Value = value; Line = line; }

        public override string ToString() => $"[{Kind} '{Value}' L{Line}]";
    }

    // ── AST Nodes ────────────────────────────────────────────────────────────────

    /** <summary>Base class for all nodes in the AI definition AST.</summary> */
    public abstract class AstNode { public int Line; }

    /** <summary>Top-level document: a list of action definitions.</summary> */
    public class DocumentNode : AstNode
    {
        public List<ActionDefNode> Actions { get; } = new();
    }

    /** <summary>
     * An action definition block:
     * <code>
     * action "name" : ActionType {
     *   param = value
     *   consideration { ... }
     * }
     * </code>
     * </summary>
     */
    public class ActionDefNode : AstNode
    {
        public string Name;
        public string ActionType;
        public List<PropertyNode> Properties { get; } = new();
        public ConsiderationNode Consideration;
    }

    /** <summary>A key=value property inside an action or consideration block.</summary> */
    public class PropertyNode : AstNode { public string Key; public ValueNode Value; }

    /** <summary>Base for all value node types.</summary> */
    public abstract class ValueNode : AstNode { }

    public class StringValueNode : ValueNode     { public string Value; }
    public class NumberValueNode : ValueNode     { public float Value; }
    public class BoolValueNode   : ValueNode     { public bool Value; }
    public class IdentifierValueNode : ValueNode { public string Name; }

    /** <summary>An array literal: [v1, v2, ...]</summary> */
    public class ArrayValueNode : ValueNode
    {
        public List<ValueNode> Elements { get; } = new();
    }

    // ── Consideration nodes ──────────────────────────────────────────────────────

    /** <summary>Base for all inline consideration AST nodes.</summary> */
    public abstract class ConsiderationNode : AstNode { }

    /** <summary>consideration constant { value = 0.5 }</summary> */
    public class ConstantConsiderationNode : ConsiderationNode { public float Value; }

    /** <summary>consideration random { min = 0, max = 1 }</summary> */
    public class RandomConsiderationNode : ConsiderationNode { public float Min; public float Max; }

    /** <summary>consideration curve { key = "float.dist_to_player_norm", points = [(0,1),(1,0)] }</summary> */
    public class CurveConsiderationNode : ConsiderationNode
    {
        public string ContextKeyId;
        /** <summary>List of (time, value) pairs defining the AnimationCurve keyframes.</summary> */
        public List<(float t, float v)> Points { get; } = new();
    }

    /** <summary>consideration inrange { key = "target.player", maxDist = 5, maxAngle = 90, points = [...] }</summary> */
    public class InRangeConsiderationNode : ConsiderationNode
    {
        public string ContextKeyId;
        public float MaxDistance;
        public float MaxAngle;
        public List<(float t, float v)> Points { get; } = new();
    }

    /** <summary>consideration bool { key = "bool.is_aggro", invert = false }</summary> */
    public class BoolConsiderationNode : ConsiderationNode { public string ContextKeyId; public bool Invert; }

    /** <summary>consideration targetexists { key = "target.player" }</summary> */
    public class TargetExistsConsiderationNode : ConsiderationNode { public string ContextKeyId; public bool Invert; }

    /** <summary>consideration threshold { key = "...", comparison = LessThan, threshold = 0.3, ifTrue = 1, ifFalse = 0 }</summary> */
    public class ThresholdConsiderationNode : ConsiderationNode
    {
        public string ContextKeyId;
        public string Comparison;
        public float Threshold;
        public float IfTrue;
        public float IfFalse;
    }

    /** <summary>consideration stringmatch { key = "...", match = "EnemyLightAttack", ifMatch = 0, ifNoMatch = 1 }</summary> */
    public class StringMatchConsiderationNode : ConsiderationNode
    {
        public string ContextKeyId;
        public string MatchValue;
        public float IfMatch;
        public float IfNoMatch;
    }

    /** <summary>
     * consideration composite {
     *   allMustBeNonZero = true
     *   first { ... }
     *   rest [ op: { ... }, op: { ... } ]
     * }
     * </summary>
     */
    public class CompositeConsiderationNode : ConsiderationNode
    {
        public bool AllMustBeNonZero;
        public ConsiderationNode First;
        public List<(string op, ConsiderationNode child)> Rest { get; } = new();
    }
}

