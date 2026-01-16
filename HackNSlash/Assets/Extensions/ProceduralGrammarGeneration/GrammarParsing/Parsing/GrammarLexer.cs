using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ProceduralGrammarGeneration.GrammarParsing
{
    /// <summary>
    /// Tokenizes grammar source text into a stream of tokens.
    /// This is the lexical analysis (lexing) stage of the grammar compiler.
    /// </summary>
    public class GrammarLexer
    {
        private readonly string _source;
        private int _position;
        private int _line;
        private int _column;

        public GrammarLexer(string source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _position = 0;
            _line = 1;
            _column = 1;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (!IsAtEnd())
            {
                SkipWhitespaceAndComments();
                if (IsAtEnd()) break;

                var token = ScanToken();
                if (token != null)
                    tokens.Add(token);
            }

            tokens.Add(new Token(TokenType.EndOfFile, "", _line, _column));
            return tokens;
        }

        private Token ScanToken()
        {
            var start = _position;
            var startLine = _line;
            var startColumn = _column;

            char c = Advance();

            // Single-character tokens
            switch (c)
            {
                case '(': return MakeToken(TokenType.LeftParen, "(", startLine, startColumn);
                case ')': return MakeToken(TokenType.RightParen, ")", startLine, startColumn);
                case '{': return MakeToken(TokenType.LeftBrace, "{", startLine, startColumn);
                case '}': return MakeToken(TokenType.RightBrace, "}", startLine, startColumn);
                case '[': return MakeToken(TokenType.LeftBracket, "[", startLine, startColumn);
                case ']': return MakeToken(TokenType.RightBracket, "]", startLine, startColumn);
                case ',': return MakeToken(TokenType.Comma, ",", startLine, startColumn);
                case ':': return MakeToken(TokenType.Colon, ":", startLine, startColumn);
                case ';': return MakeToken(TokenType.Semicolon, ";", startLine, startColumn);
                case '+': return MakeToken(TokenType.Plus, "+", startLine, startColumn);
                case '-': return MakeToken(TokenType.Minus, "-", startLine, startColumn);
                case '*': return MakeToken(TokenType.Star, "*", startLine, startColumn);
                case '/': return MakeToken(TokenType.Slash, "/", startLine, startColumn);
            }

            // Two-character operators
            if (c == '=' && Match('='))
                return MakeToken(TokenType.EqualEqual, "==", startLine, startColumn);
            if (c == '!' && Match('='))
                return MakeToken(TokenType.BangEqual, "!=", startLine, startColumn);
            if (c == '<' && Match('='))
                return MakeToken(TokenType.LessEqual, "<=", startLine, startColumn);
            if (c == '>' && Match('='))
                return MakeToken(TokenType.GreaterEqual, ">=", startLine, startColumn);
            if (c == '=' && Match('>'))
                return MakeToken(TokenType.Arrow, "=>", startLine, startColumn);
            if (c == '&' && Match('&'))
                return MakeToken(TokenType.And, "&&", startLine, startColumn);
            if (c == '|' && Match('|'))
                return MakeToken(TokenType.Or, "||", startLine, startColumn);

            // Single '=' (assignment in parameter definitions)
            if (c == '=')
                return MakeToken(TokenType.Equal, "=", startLine, startColumn);
            if (c == '<')
                return MakeToken(TokenType.Less, "<", startLine, startColumn);
            if (c == '>')
                return MakeToken(TokenType.Greater, ">", startLine, startColumn);
            if (c == '!')
                return MakeToken(TokenType.Bang, "!", startLine, startColumn);

            // String literals
            if (c == '"')
                return ScanString(startLine, startColumn);

            // Numbers
            if (char.IsDigit(c) || (c == '.' && char.IsDigit(Peek())))
                return ScanNumber(start, startLine, startColumn);

            // Identifiers and keywords
            if (char.IsLetter(c) || c == '_')
                return ScanIdentifierOrKeyword(start, startLine, startColumn);

            throw new LexerException($"Unexpected character '{c}' at line {startLine}, column {startColumn}");
        }

        private Token ScanString(int startLine, int startColumn)
        {
            var sb = new StringBuilder();

            while (!IsAtEnd() && Peek() != '"')
            {
                if (Peek() == '\\')
                {
                    Advance(); // Skip backslash
                    if (IsAtEnd())
                        throw new LexerException($"Unterminated string at line {startLine}, column {startColumn}");

                    char escaped = Advance();
                    switch (escaped)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case '\\': sb.Append('\\'); break;
                        case '"': sb.Append('"'); break;
                        default: sb.Append(escaped); break;
                    }
                }
                else
                {
                    sb.Append(Advance());
                }
            }

            if (IsAtEnd())
                throw new LexerException($"Unterminated string at line {startLine}, column {startColumn}");

            Advance(); // Closing quote
            return MakeToken(TokenType.String, sb.ToString(), startLine, startColumn);
        }

        private Token ScanNumber(int start, int startLine, int startColumn)
        {
            while (char.IsDigit(Peek()))
                Advance();

            // Check for decimal part
            bool isFloat = false;
            if (Peek() == '.' && char.IsDigit(PeekNext()))
            {
                isFloat = true;
                Advance(); // Consume '.'
                while (char.IsDigit(Peek()))
                    Advance();
            }

            // Check for 'r' suffix (relative size)
            bool isRelative = false;
            if (Peek() == 'r')
            {
                isRelative = true;
                Advance();
            }

            string lexeme = _source.Substring(start, _position - start);
            if (isRelative)
                lexeme = lexeme.Substring(0, lexeme.Length - 1); // Remove 'r'

            var tokenType = isFloat ? TokenType.Float : TokenType.Integer;
            var token = MakeToken(tokenType, lexeme, startLine, startColumn);
            
            if (isRelative)
                token.IsRelative = true;

            return token;
        }

        private Token ScanIdentifierOrKeyword(int start, int startLine, int startColumn)
        {
            while (char.IsLetterOrDigit(Peek()) || Peek() == '_')
                Advance();

            string lexeme = _source.Substring(start, _position - start);
            var tokenType = GetKeywordType(lexeme);
            return MakeToken(tokenType, lexeme, startLine, startColumn);
        }

        private TokenType GetKeywordType(string text)
        {
            return text switch
            {
                "rule" => TokenType.Rule,
                "when" => TokenType.When,
                "if" => TokenType.If,
                "split" => TokenType.Split,
                "repeat" => TokenType.Repeat,
                "choose" => TokenType.Choose,
                "default" => TokenType.Default,
                "option" => TokenType.Option,
                "weight" => TokenType.Weight,
                "symbol" => TokenType.Symbol,
                "terminal" => TokenType.Terminal,
                "true" => TokenType.True,
                "false" => TokenType.False,
                "float" => TokenType.FloatType,
                "int" => TokenType.IntType,
                "string" => TokenType.StringType,
                "bool" => TokenType.BoolType,
                "X" => TokenType.X,
                "Y" => TokenType.Y,
                "Z" => TokenType.Z,
                _ => TokenType.Identifier
            };
        }

        private void SkipWhitespaceAndComments()
        {
            while (!IsAtEnd())
            {
                char c = Peek();

                if (char.IsWhiteSpace(c))
                {
                    if (c == '\n')
                    {
                        _line++;
                        _column = 1;
                    }
                    else
                    {
                        _column++;
                    }
                    _position++;
                }
                else if (c == '/' && PeekNext() == '/')
                {
                    // Single-line comment
                    while (!IsAtEnd() && Peek() != '\n')
                        Advance();
                }
                else if (c == '/' && PeekNext() == '*')
                {
                    // Multi-line comment
                    Advance(); // '/'
                    Advance(); // '*'
                    while (!IsAtEnd())
                    {
                        if (Peek() == '*' && PeekNext() == '/')
                        {
                            Advance(); // '*'
                            Advance(); // '/'
                            break;
                        }
                        Advance();
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private char Advance()
        {
            if (IsAtEnd()) return '\0';
            _column++;
            return _source[_position++];
        }

        private bool Match(char expected)
        {
            if (IsAtEnd()) return false;
            if (_source[_position] != expected) return false;
            _position++;
            _column++;
            return true;
        }

        private char Peek()
        {
            if (IsAtEnd()) return '\0';
            return _source[_position];
        }

        private char PeekNext()
        {
            if (_position + 1 >= _source.Length) return '\0';
            return _source[_position + 1];
        }

        private bool IsAtEnd() => _position >= _source.Length;

        private Token MakeToken(TokenType type, string lexeme, int line, int column)
        {
            return new Token(type, lexeme, line, column);
        }
    }

    /// <summary>
    /// Represents a single token in the grammar source.
    /// </summary>
    public class Token
    {
        public TokenType Type { get; }
        public string Lexeme { get; }
        public int Line { get; }
        public int Column { get; }
        public bool IsRelative { get; set; } // For relative size notation (e.g., "2.5r")

        public Token(TokenType type, string lexeme, int line, int column)
        {
            Type = type;
            Lexeme = lexeme;
            Line = line;
            Column = column;
        }

        public override string ToString() => $"{Type}({Lexeme}) at {Line}:{Column}";
    }

    /// <summary>
    /// All possible token types in the grammar language.
    /// </summary>
    public enum TokenType
    {
        // Literals
        Identifier,
        Integer,
        Float,
        String,
        True,
        False,

        // Keywords
        Rule,
        When,
        If,
        Split,
        Repeat,
        Choose,
        Default,
        Option,
        Weight,
        Symbol,
        Terminal,

        // Type keywords
        FloatType,
        IntType,
        StringType,
        BoolType,

        // Axis identifiers
        X,
        Y,
        Z,

        // Operators
        Plus,
        Minus,
        Star,
        Slash,
        Equal,
        EqualEqual,
        BangEqual,
        Less,
        LessEqual,
        Greater,
        GreaterEqual,
        And,
        Or,
        Bang,
        Arrow,

        // Delimiters
        LeftParen,
        RightParen,
        LeftBrace,
        RightBrace,
        LeftBracket,
        RightBracket,
        Comma,
        Colon,
        Semicolon,

        // Special
        EndOfFile
    }

    /// <summary>
    /// Exception thrown during lexical analysis.
    /// </summary>
    public class LexerException : Exception
    {
        public LexerException(string message) : base(message) { }
    }
}
