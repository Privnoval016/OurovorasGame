using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralGrammarGeneration.GrammarParsing
{
    /// <summary>
    /// Parses a token stream into a grammar AST (Abstract Syntax Tree).
    /// This is the syntactic analysis (parsing) stage of the grammar compiler.
    /// Validates grammar structure and produces a parse tree.
    /// </summary>
    public class GrammarParser
    {
        private readonly List<Token> _tokens;
        private int _current;
        private int _nextSymbolId;
        private int _nextRuleId;

        public GrammarParser(List<Token> tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            _current = 0;
            _nextSymbolId = 0;
            _nextRuleId = 0;
        }

        public GrammarDefinition Parse()
        {
            var grammar = new GrammarDefinition();
            var symbolRegistry = new Dictionary<string, SymbolType>();

            // First pass: collect all symbol declarations
            while (!IsAtEnd())
            {
                if (Check(TokenType.Symbol))
                {
                    var symbolDef = ParseSymbolDeclaration(symbolRegistry);
                    grammar.Symbols.Add(symbolDef);
                }
                else if (Check(TokenType.Rule))
                {
                    var rule = ParseRule(symbolRegistry);
                    grammar.Rules.Add(rule);
                }
                else
                {
                    throw new ParserException($"Unexpected token '{Current.Lexeme}' at {Current.Line}:{Current.Column}. Expected 'symbol' or 'rule'.");
                }
            }

            // Validate that grammar has at least one rule
            if (grammar.Rules.Count == 0)
                throw new ParserException("Grammar must contain at least one rule");

            // Set entry symbol (first rule's input symbol by default)
            if (grammar.Rules.Count > 0)
                grammar.EntrySymbol = grammar.Rules[0].InputSymbol;

            return grammar;
        }

        #region Symbol Declaration Parsing

        private SymbolDefinition ParseSymbolDeclaration(Dictionary<string, SymbolType> symbolRegistry)
        {
            Consume(TokenType.Symbol, "Expected 'symbol'");

            bool isTerminal = false;
            if (Match(TokenType.Terminal))
                isTerminal = true;

            var name = Consume(TokenType.Identifier, "Expected symbol name").Lexeme;

            // Check if symbol already declared
            if (symbolRegistry.ContainsKey(name))
                throw new ParserException($"Symbol '{name}' already declared at {Current.Line}:{Current.Column}");

            var symbolType = new SymbolType(name, _nextSymbolId++);
            symbolRegistry[name] = symbolType;

            var symbolDef = new SymbolDefinition
            {
                Type = symbolType,
                IsTerminal = isTerminal
            };

            // Parse parameters if present
            if (Match(TokenType.LeftParen))
            {
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        var paramDef = ParseParameterDefinition();
                        symbolDef.Parameters.Add(paramDef);
                    }
                    while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after parameters");
            }

            // Optional semicolon
            Match(TokenType.Semicolon);

            return symbolDef;
        }

        private ParameterDefinition ParseParameterDefinition()
        {
            var name = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
            Consume(TokenType.Colon, "Expected ':' after parameter name");
            var type = ParseParameterType();

            bool isRequired = true;
            object defaultValue = null;

            if (Match(TokenType.Equal))
            {
                isRequired = false;
                defaultValue = ParseLiteralValue();
            }

            return new ParameterDefinition(name, type, isRequired, defaultValue);
        }

        private ParameterType ParseParameterType()
        {
            if (Match(TokenType.FloatType)) return ParameterType.Float;
            if (Match(TokenType.IntType)) return ParameterType.Int;
            if (Match(TokenType.StringType)) return ParameterType.String;
            if (Match(TokenType.BoolType)) return ParameterType.Bool;

            // Check for identifier types (e.g., Vector3, Color)
            if (Check(TokenType.Identifier))
            {
                var typeName = Advance().Lexeme;
                return typeName switch
                {
                    "Vector3" => ParameterType.Vector3,
                    "Color" => ParameterType.Color,
                    _ => throw new ParserException($"Unknown parameter type '{typeName}' at {Previous.Line}:{Previous.Column}")
                };
            }

            throw new ParserException($"Expected parameter type at {Current.Line}:{Current.Column}");
        }

        #endregion

        #region Rule Parsing

        private Rule ParseRule(Dictionary<string, SymbolType> symbolRegistry)
        {
            Consume(TokenType.Rule, "Expected 'rule'");

            var rule = new Rule
            {
                Id = _nextRuleId++,
                Name = Consume(TokenType.Identifier, "Expected rule name").Lexeme
            };

            Consume(TokenType.When, "Expected 'when' after rule name");

            // Parse input symbol
            var symbolName = Consume(TokenType.Identifier, "Expected symbol name").Lexeme;
            if (!symbolRegistry.TryGetValue(symbolName, out var symbolType))
                throw new ParserException($"Undefined symbol '{symbolName}' at {Previous.Line}:{Previous.Column}");

            rule.InputSymbol = symbolType;

            // Parse parameters
            if (Match(TokenType.LeftParen))
            {
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        var paramDef = ParseRuleParameter();
                        rule.Parameters.Add(paramDef);
                    }
                    while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after parameters");
            }

            // Parse optional condition
            if (Match(TokenType.If))
            {
                rule.Condition = ParseCondition();
            }

            Consume(TokenType.Arrow, "Expected '=>' before rule expansion");

            // Parse expansion
            rule.Expansion = ParseExpansion(symbolRegistry);

            // Optional semicolon
            Match(TokenType.Semicolon);

            return rule;
        }

        private ParameterDefinition ParseRuleParameter()
        {
            var name = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;

            // Rule parameters are untyped (type inferred from context)
            return new ParameterDefinition(name, ParameterType.Float, true);
        }

        #endregion

        #region Condition Parsing

        private Condition ParseCondition()
        {
            return ParseOrCondition();
        }

        private Condition ParseOrCondition()
        {
            var left = ParseAndCondition();

            while (Match(TokenType.Or))
            {
                var right = ParseAndCondition();
                left = new LogicalCondition
                {
                    Operator = LogicalOperator.Or,
                    Operands = new List<Condition> { left, right }
                };
            }

            return left;
        }

        private Condition ParseAndCondition()
        {
            var left = ParseUnaryCondition();

            while (Match(TokenType.And))
            {
                var right = ParseUnaryCondition();
                left = new LogicalCondition
                {
                    Operator = LogicalOperator.And,
                    Operands = new List<Condition> { left, right }
                };
            }

            return left;
        }

        private Condition ParseUnaryCondition()
        {
            if (Match(TokenType.Bang))
            {
                var operand = ParseUnaryCondition();
                return new LogicalCondition
                {
                    Operator = LogicalOperator.Not,
                    Operands = new List<Condition> { operand }
                };
            }

            if (Match(TokenType.LeftParen))
            {
                var condition = ParseCondition();
                Consume(TokenType.RightParen, "Expected ')' after condition");
                return condition;
            }

            return ParseComparisonCondition();
        }

        private Condition ParseComparisonCondition()
        {
            var leftParam = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;

            var op = Current.Type switch
            {
                TokenType.EqualEqual => ComparisonOperator.Equal,
                TokenType.BangEqual => ComparisonOperator.NotEqual,
                TokenType.Less => ComparisonOperator.LessThan,
                TokenType.LessEqual => ComparisonOperator.LessOrEqual,
                TokenType.Greater => ComparisonOperator.GreaterThan,
                TokenType.GreaterEqual => ComparisonOperator.GreaterOrEqual,
                _ => throw new ParserException($"Expected comparison operator at {Current.Line}:{Current.Column}")
            };
            Advance();

            var rightValue = ParseLiteralValue();

            return new ComparisonCondition
            {
                LeftOperand = leftParam,
                Operator = op,
                RightOperand = rightValue
            };
        }

        #endregion

        #region Expansion Parsing

        private Expansion ParseExpansion(Dictionary<string, SymbolType> symbolRegistry)
        {
            if (Check(TokenType.Split))
                return ParseSplitExpansion(symbolRegistry);

            if (Check(TokenType.Repeat))
                return ParseRepeatExpansion(symbolRegistry);

            if (Check(TokenType.Choose))
                return ParseChooseExpansion(symbolRegistry);

            return ParseProductionExpansion(symbolRegistry);
        }

        private ProductionExpansion ParseProductionExpansion(Dictionary<string, SymbolType> symbolRegistry)
        {
            var expansion = new ProductionExpansion();

            while (!Check(TokenType.Semicolon) && !Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var symbolInstance = ParseSymbolInstance(symbolRegistry);
                expansion.Symbols.Add(symbolInstance);
                
                // Accept optional comma separator (whitespace agnostic)
                Match(TokenType.Comma);
                
                // Stop if we hit a terminator or there's no identifier following
                if (Check(TokenType.Semicolon) || Check(TokenType.RightBrace) || !Check(TokenType.Identifier))
                    break;
            }

            return expansion;
        }

        private SymbolInstance ParseSymbolInstance(Dictionary<string, SymbolType> symbolRegistry)
        {
            var symbolName = Consume(TokenType.Identifier, "Expected symbol name").Lexeme;

            if (!symbolRegistry.TryGetValue(symbolName, out var symbolType))
                throw new ParserException($"Undefined symbol '{symbolName}' at {Previous.Line}:{Previous.Column}");

            var instance = new SymbolInstance(symbolType);

            if (Match(TokenType.LeftParen))
            {
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        var paramName = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
                        Consume(TokenType.Equal, "Expected '=' after parameter name");
                        var expr = ParseExpression();
                        instance.ParameterExpressions[paramName] = expr;
                    }
                    while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after parameters");
            }

            return instance;
        }

        private SplitExpansion ParseSplitExpansion(Dictionary<string, SymbolType> symbolRegistry)
        {
            Consume(TokenType.Split, "Expected 'split'");
            Consume(TokenType.LeftParen, "Expected '(' after 'split'");

            var axis = ParseAxis();

            Consume(TokenType.RightParen, "Expected ')' after axis");
            Consume(TokenType.LeftBrace, "Expected '{' after 'split(...)'");

            var expansion = new SplitExpansion { Axis = axis };

            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var part = ParseSplitPart(symbolRegistry);
                expansion.Parts.Add(part);
            }

            Consume(TokenType.RightBrace, "Expected '}' after split parts");

            return expansion;
        }

        private SplitPart ParseSplitPart(Dictionary<string, SymbolType> symbolRegistry)
        {
            var sizeExpr = ParseExpression();
            
            // Check for 'r' suffix or relative marker
            var mode = SizeMode.Absolute;
            if (Previous.Type == TokenType.Identifier && Previous.Lexeme == "r")
                mode = SizeMode.Relative;

            Consume(TokenType.Colon, "Expected ':' after split size");

            var content = ParseExpansion(symbolRegistry);

            return new SplitPart
            {
                Mode = mode,
                Size = sizeExpr,
                Expansion = content
            };
        }

        private RepeatExpansion ParseRepeatExpansion(Dictionary<string, SymbolType> symbolRegistry)
        {
            Consume(TokenType.Repeat, "Expected 'repeat'");
            Consume(TokenType.LeftParen, "Expected '(' after 'repeat'");

            var axis = ParseAxis();
            Consume(TokenType.Comma, "Expected ',' after axis");

            var size = ParseExpression();

            Consume(TokenType.RightParen, "Expected ')' after size");
            Consume(TokenType.LeftBrace, "Expected '{' after 'repeat(...)'");

            var content = ParseExpansion(symbolRegistry);

            Consume(TokenType.RightBrace, "Expected '}' after repeat content");

            return new RepeatExpansion
            {
                Axis = axis,
                Size = size,
                Content = content
            };
        }

        private ChooseExpansion ParseChooseExpansion(Dictionary<string, SymbolType> symbolRegistry)
        {
            Consume(TokenType.Choose, "Expected 'choose'");
            Consume(TokenType.LeftBrace, "Expected '{' after 'choose'");

            var expansion = new ChooseExpansion();
            int optionIndex = 0;

            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var option = ParseChoiceOption(symbolRegistry);
                expansion.Options.Add(option);

                if (option.IsDefault)
                    expansion.DefaultOptionIndex = optionIndex;

                optionIndex++;
            }

            Consume(TokenType.RightBrace, "Expected '}' after choose options");

            // If no default specified, first option is default
            if (expansion.DefaultOptionIndex == -1 && expansion.Options.Count > 0)
                expansion.DefaultOptionIndex = 0;

            return expansion;
        }

        private ChoiceOption ParseChoiceOption(Dictionary<string, SymbolType> symbolRegistry)
        {
            var option = new ChoiceOption { Weight = 1.0f };

            if (Match(TokenType.Default))
            {
                option.IsDefault = true;
            }
            else if (Match(TokenType.Weight))
            {
                var weightExpr = ParseExpression();
                if (weightExpr is LiteralExpression lit && lit.Value is float weight)
                    option.Weight = weight;
                else
                    throw new ParserException($"Weight must be a float literal at {Previous.Line}:{Previous.Column}");
            }
            else if (Match(TokenType.Option))
            {
                // Just 'option:' without weight
                option.Weight = 1.0f;
            }

            Consume(TokenType.Colon, "Expected ':' after option specifier");

            option.Expansion = ParseExpansion(symbolRegistry);

            return option;
        }

        private SplitAxis ParseAxis()
        {
            if (Match(TokenType.X)) return SplitAxis.X;
            if (Match(TokenType.Y)) return SplitAxis.Y;
            if (Match(TokenType.Z)) return SplitAxis.Z;

            throw new ParserException($"Expected axis (X, Y, or Z) at {Current.Line}:{Current.Column}");
        }

        #endregion

        #region Expression Parsing

        private Expression ParseExpression()
        {
            return ParseAdditive();
        }

        private Expression ParseAdditive()
        {
            var left = ParseMultiplicative();

            while (Match(TokenType.Plus) || Match(TokenType.Minus))
            {
                var op = Previous.Type == TokenType.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
                var right = ParseMultiplicative();
                left = new BinaryExpression
                {
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }

        private Expression ParseMultiplicative()
        {
            var left = ParseUnary();

            while (Match(TokenType.Star) || Match(TokenType.Slash))
            {
                var op = Previous.Type == TokenType.Star ? BinaryOperator.Multiply : BinaryOperator.Divide;
                var right = ParseUnary();
                left = new BinaryExpression
                {
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }

        private Expression ParseUnary()
        {
            if (Match(TokenType.Minus))
            {
                var operand = ParseUnary();
                return new BinaryExpression
                {
                    Left = new LiteralExpression(-1),
                    Operator = BinaryOperator.Multiply,
                    Right = operand
                };
            }

            return ParsePrimary();
        }

        private Expression ParsePrimary()
        {
            if (Match(TokenType.Integer))
            {
                var value = int.Parse(Previous.Lexeme);
                return new LiteralExpression(value);
            }

            if (Match(TokenType.Float))
            {
                var value = float.Parse(Previous.Lexeme);
                return new LiteralExpression(value);
            }

            if (Match(TokenType.String))
            {
                return new LiteralExpression(Previous.Lexeme);
            }

            if (Match(TokenType.True))
                return new LiteralExpression(true);

            if (Match(TokenType.False))
                return new LiteralExpression(false);

            if (Match(TokenType.Identifier))
            {
                return new ParameterExpression(Previous.Lexeme);
            }

            if (Match(TokenType.LeftParen))
            {
                var expr = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after expression");
                return expr;
            }

            throw new ParserException($"Expected expression at {Current.Line}:{Current.Column}");
        }

        private object ParseLiteralValue()
        {
            if (Match(TokenType.Integer))
                return int.Parse(Previous.Lexeme);

            if (Match(TokenType.Float))
                return float.Parse(Previous.Lexeme);

            if (Match(TokenType.String))
                return Previous.Lexeme;

            if (Match(TokenType.True))
                return true;

            if (Match(TokenType.False))
                return false;

            throw new ParserException($"Expected literal value at {Current.Line}:{Current.Column}");
        }

        #endregion

        #region Helper Methods

        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Current.Type == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) _current++;
            return Previous;
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw new ParserException($"{message} at {Current.Line}:{Current.Column}. Got '{Current.Lexeme}'");
        }

        private bool IsAtEnd() => Current.Type == TokenType.EndOfFile;

        private Token Current => _tokens[_current];
        private Token Previous => _tokens[_current - 1];

        #endregion
    }

    /// <summary>
    /// Exception thrown during parsing.
    /// </summary>
    public class ParserException : Exception
    {
        public ParserException(string message) : base(message) { }
    }
}
