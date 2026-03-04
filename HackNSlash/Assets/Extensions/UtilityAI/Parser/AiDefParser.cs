using System;
using System.Collections.Generic;

namespace Extensions.UtilityAI.Parser
{
    /** <summary>
     * Recursive-descent parser for the AI definition language.
     * Consumes the token list produced by <see cref="Lexer"/> and
     * builds a <see cref="DocumentNode"/> AST.
     *
     * Grammar (simplified EBNF):
     * <code>
     * document     = action* EOF
     * action       = 'action' STRING ':' IDENT '{' action_body '}'
     * action_body  = (property | consideration_stmt)*
     * property     = IDENT '=' value NEWLINE?
     * consideration_stmt = 'consideration' consideration_block
     * consideration_block = consideration_type '{' ... '}'
     * value        = STRING | NUMBER | BOOL | IDENT | array
     * array        = '[' (value (',' value)*)? ']'
     * </code>
     * </summary>
     */
    public sealed class AiDefParser
    {
        private readonly List<Token> _tokens;
        private int _pos;

        public AiDefParser(List<Token> tokens) { _tokens = tokens; _pos = 0; }

        /** <summary>Parse the full token stream and return the document AST.</summary> */
        public DocumentNode ParseDocument()
        {
            var doc = new DocumentNode { Line = 1 };
            SkipNewlines();
            while (Current.Kind != TokenKind.EOF)
            {
                doc.Actions.Add(ParseAction());
                SkipNewlines();
            }
            return doc;
        }

        // ── Action ────────────────────────────────────────────────────────────

        private ActionDefNode ParseAction()
        {
            int line = Current.Line;
            ExpectIdentifier("action");
            SkipNewlines();
            string name = ExpectValue(TokenKind.String);
            SkipNewlines();
            Expect(TokenKind.Colon);
            SkipNewlines();
            string type = ExpectAnyIdentifier();
            SkipNewlines();
            Expect(TokenKind.LeftBrace);
            SkipNewlines();

            var node = new ActionDefNode { Name = name, ActionType = type, Line = line };

            while (Current.Kind != TokenKind.RightBrace && Current.Kind != TokenKind.EOF)
            {
                SkipNewlines();
                if (Current.Kind == TokenKind.RightBrace) break;

                if (CurrentIs("consideration"))
                {
                    Advance();
                    node.Consideration = ParseConsideration();
                }
                else
                {
                    node.Properties.Add(ParseProperty());
                }
                SkipNewlines();
            }
            SkipNewlines();
            Expect(TokenKind.RightBrace);
            return node;
        }

        // ── Properties ────────────────────────────────────────────────────────

        private PropertyNode ParseProperty()
        {
            int line = Current.Line;
            string key = ExpectAnyIdentifier();
            SkipNewlines();
            Expect(TokenKind.Equals);
            SkipNewlines();
            ValueNode val = ParseValue();
            SkipNewlines();
            return new PropertyNode { Key = key, Value = val, Line = line };
        }

        private ValueNode ParseValue()
        {
            int line = Current.Line;
            return Current.Kind switch
            {
                TokenKind.String     => new StringValueNode { Value = Advance().Value, Line = line },
                TokenKind.Number     => new NumberValueNode { Value = float.Parse(Advance().Value,
                                            System.Globalization.CultureInfo.InvariantCulture), Line = line },
                TokenKind.Bool       => new BoolValueNode { Value = Advance().Value == "true", Line = line },
                TokenKind.Identifier => new IdentifierValueNode { Name = Advance().Value, Line = line },
                TokenKind.LeftBracket => ParseArray(),
                _ => throw new ParseException($"Expected a value but got {Current}", Current.Line)
            };
        }

        private ArrayValueNode ParseArray()
        {
            int line = Current.Line;
            Expect(TokenKind.LeftBracket);
            var arr = new ArrayValueNode { Line = line };
            SkipNewlines();
            while (Current.Kind != TokenKind.RightBracket && Current.Kind != TokenKind.EOF)
            {
                arr.Elements.Add(ParseValue());
                SkipNewlines();
                if (Current.Kind == TokenKind.Comma) { Advance(); SkipNewlines(); }
            }
            SkipNewlines();
            Expect(TokenKind.RightBracket);
            return arr;
        }

        // ── Considerations ────────────────────────────────────────────────────

        private ConsiderationNode ParseConsideration()
        {
            int line = Current.Line;
            string kind = ExpectAnyIdentifier().ToLowerInvariant();
            SkipNewlines();
            Expect(TokenKind.LeftBrace);
            SkipNewlines();

            ConsiderationNode node = kind switch
            {
                "constant"     => ParseConstant(line),
                "random"       => ParseRandom(line),
                "curve"        => ParseCurve(line),
                "inrange"      => ParseInRange(line),
                "bool"         => ParseBool(line),
                "targetexists" => ParseTargetExists(line),
                "threshold"    => ParseThreshold(line),
                "stringmatch"  => ParseStringMatch(line),
                "composite"    => ParseComposite(line),
                _ => throw new ParseException($"Unknown consideration type '{kind}'", line)
            };

            SkipNewlines();
            Expect(TokenKind.RightBrace);
            return node;
        }

        private ConstantConsiderationNode ParseConstant(int line)
        {
            var props = ParsePropertyDict();
            return new ConstantConsiderationNode { Value = GetFloat(props, "value", 0.5f), Line = line };
        }

        private RandomConsiderationNode ParseRandom(int line)
        {
            var props = ParsePropertyDict();
            return new RandomConsiderationNode { Min = GetFloat(props, "min", 0f), Max = GetFloat(props, "max", 1f), Line = line };
        }

        private CurveConsiderationNode ParseCurve(int line)
        {
            var props = ParsePropertyDict();
            var node = new CurveConsiderationNode { ContextKeyId = GetString(props, "key"), Line = line };
            node.Points.AddRange(GetCurvePoints(props, "points"));
            return node;
        }

        private InRangeConsiderationNode ParseInRange(int line)
        {
            var props = ParsePropertyDict();
            var node = new InRangeConsiderationNode
            {
                ContextKeyId = GetString(props, "key"),
                MaxDistance  = GetFloat(props, "maxdist", 10f),
                MaxAngle     = GetFloat(props, "maxangle", 360f),
                Line = line
            };
            node.Points.AddRange(GetCurvePoints(props, "points"));
            return node;
        }

        private BoolConsiderationNode ParseBool(int line)
        {
            var props = ParsePropertyDict();
            return new BoolConsiderationNode { ContextKeyId = GetString(props, "key"), Invert = GetBool(props, "invert", false), Line = line };
        }

        private TargetExistsConsiderationNode ParseTargetExists(int line)
        {
            var props = ParsePropertyDict();
            return new TargetExistsConsiderationNode { ContextKeyId = GetString(props, "key"), Invert = GetBool(props, "invert", false), Line = line };
        }

        private ThresholdConsiderationNode ParseThreshold(int line)
        {
            var props = ParsePropertyDict();
            return new ThresholdConsiderationNode
            {
                ContextKeyId = GetString(props, "key"),
                Comparison   = GetString(props, "comparison", "lessthan"),
                Threshold    = GetFloat(props, "threshold", 0.5f),
                IfTrue       = GetFloat(props, "iftrue", 1f),
                IfFalse      = GetFloat(props, "iffalse", 0f),
                Line = line
            };
        }

        private StringMatchConsiderationNode ParseStringMatch(int line)
        {
            var props = ParsePropertyDict();
            return new StringMatchConsiderationNode
            {
                ContextKeyId = GetString(props, "key"),
                MatchValue   = GetString(props, "match"),
                IfMatch      = GetFloat(props, "ifmatch", 0f),
                IfNoMatch    = GetFloat(props, "ifnomatch", 1f),
                Line = line
            };
        }

        private CompositeConsiderationNode ParseComposite(int line)
        {
            var props = ParsePropertyDict();
            bool allMust = GetBool(props, "allMustBeNonZero", false);

            SkipNewlines();
            ExpectIdentifier("first");
            SkipNewlines();
            var first = ParseConsideration();

            SkipNewlines();
            var rest = new List<(string, ConsiderationNode)>();
            if (CurrentIs("rest"))
            {
                Advance();
                SkipNewlines();
                Expect(TokenKind.LeftBracket);
                SkipNewlines();
                while (Current.Kind != TokenKind.RightBracket && Current.Kind != TokenKind.EOF)
                {
                    SkipNewlines();
                    if (Current.Kind == TokenKind.RightBracket) break;
                    string op = ExpectAnyIdentifier().ToLowerInvariant();
                    SkipNewlines();
                    Expect(TokenKind.Colon);
                    SkipNewlines();
                    rest.Add((op, ParseConsideration()));
                    SkipNewlines();
                    if (Current.Kind == TokenKind.Comma) { Advance(); SkipNewlines(); }
                }
                SkipNewlines();
                Expect(TokenKind.RightBracket);
            }

            var node = new CompositeConsiderationNode { AllMustBeNonZero = allMust, First = first, Line = line };
            node.Rest.AddRange(rest);
            return node;
        }

        // ── Property dict helper (parses key=value pairs until a block terminator) ──

        private Dictionary<string, ValueNode> ParsePropertyDict()
        {
            var dict = new Dictionary<string, ValueNode>(StringComparer.OrdinalIgnoreCase);
            SkipNewlines();
            while (!IsPropertyDictEnd())
            {
                if (Current.Kind == TokenKind.Newline) { Advance(); continue; }
                var prop = ParseProperty();
                dict[prop.Key] = prop.Value;
                SkipNewlines();
            }
            return dict;
        }

        private bool IsPropertyDictEnd()
        {
            if (Current.Kind == TokenKind.RightBrace
                || Current.Kind == TokenKind.RightBracket
                || Current.Kind == TokenKind.EOF)
                return true;
            if (Current.Kind == TokenKind.Identifier)
            {
                string v = Current.Value.ToLowerInvariant();
                return v == "first" || v == "rest" || v == "consideration";
            }
            return false;
        }

        // ── Value extraction helpers ─────────────────────────────────────────

        private static float GetFloat(Dictionary<string, ValueNode> d, string key, float def = 0f)
            => d.TryGetValue(key, out var v) && v is NumberValueNode n ? n.Value : def;

        private static bool GetBool(Dictionary<string, ValueNode> d, string key, bool def = false)
            => d.TryGetValue(key, out var v) && v is BoolValueNode b ? b.Value : def;

        private static string GetString(Dictionary<string, ValueNode> d, string key, string def = "")
        {
            if (!d.TryGetValue(key, out var v)) return def;
            return v switch
            {
                StringValueNode sv     => sv.Value,
                IdentifierValueNode iv => iv.Name,
                _                      => def
            };
        }

        private static List<(float, float)> GetCurvePoints(Dictionary<string, ValueNode> d, string key)
        {
            var result = new List<(float, float)>();
            if (!d.TryGetValue(key, out var v) || v is not ArrayValueNode arr) return result;
            foreach (var el in arr.Elements)
            {
                if (el is ArrayValueNode pair && pair.Elements.Count == 2
                    && pair.Elements[0] is NumberValueNode t && pair.Elements[1] is NumberValueNode val)
                    result.Add((t.Value, val.Value));
            }
            return result;
        }

        // ── Token navigation helpers ─────────────────────────────────────────

        private Token Current => _tokens[Math.Min(_pos, _tokens.Count - 1)];
        private Token Advance() { var t = Current; if (_pos < _tokens.Count - 1) _pos++; return t; }

        private void Expect(TokenKind kind)
        {
            if (Current.Kind == kind) { Advance(); return; }
            throw new ParseException($"Expected {kind} but got {Current}", Current.Line);
        }

        private string ExpectValue(TokenKind kind)
        {
            if (Current.Kind != kind)
                throw new ParseException($"Expected {kind} but got {Current}", Current.Line);
            return Advance().Value;
        }

        private string ExpectAnyIdentifier()
        {
            if (Current.Kind != TokenKind.Identifier && Current.Kind != TokenKind.Bool)
                throw new ParseException($"Expected identifier but got {Current}", Current.Line);
            return Advance().Value;
        }

        private void ExpectIdentifier(string value)
        {
            if (Current.Kind != TokenKind.Identifier || !string.Equals(Current.Value, value, StringComparison.OrdinalIgnoreCase))
                throw new ParseException($"Expected identifier '{value}' but got {Current}", Current.Line);
            Advance();
        }

        private bool CurrentIs(string value)
            => Current.Kind == TokenKind.Identifier &&
               string.Equals(Current.Value, value, StringComparison.OrdinalIgnoreCase);

        private void SkipNewlines() { while (Current.Kind == TokenKind.Newline) Advance(); }
    }

    /** <summary>Thrown by <see cref="AiDefParser"/> when the input violates the grammar.</summary> */
    public sealed class ParseException : Exception
    {
        public int Line { get; }
        public ParseException(string message, int line) : base($"[L{line}] {message}") { Line = line; }
    }
}













