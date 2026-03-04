using System;
using System.Collections.Generic;
using System.Text;

namespace Extensions.UtilityAI.Parser
{
    /** <summary>
     * Lexer for the AI definition language.
     * Converts raw source text into a flat list of <see cref="Token"/> objects
     * for consumption by <see cref="AiDefParser"/>.
     *
     * Language rules:
     * - Line comments: <c>// ...</c> and <c># ...</c>
     * - Block comments: <c>/* ... *&#47;</c>
     * - Strings: <c>"..."</c> or <c>'...'</c>
     * - Numbers: integers and decimals, optional leading minus
     * - Booleans: <c>true</c> / <c>false</c>
     * - Identifiers: [a-zA-Z_][a-zA-Z0-9_.]*
     * - All other single-character punctuation maps to its <see cref="TokenKind"/>
     * </summary>
     */
    public sealed class Lexer
    {
        private readonly string _src;
        private int _pos;
        private int _line;

        public Lexer(string source)
        {
            _src = source ?? string.Empty;
            _pos = 0;
            _line = 1;
        }

        /** <summary>Lex the entire source and return a token list ending in EOF.</summary> */
        public List<Token> Tokenize()
        {
            var tokens = new List<Token>(256);
            while (_pos < _src.Length)
            {
                SkipWhitespaceAndComments();
                if (_pos >= _src.Length) break;

                char c = _src[_pos];

                if (c == '\n') { tokens.Add(new Token(TokenKind.Newline, "\n", _line)); _line++; _pos++; continue; }
                if (c == '\r') { _pos++; continue; }

                // Single-char tokens.
                TokenKind? simple = c switch
                {
                    '{' => TokenKind.LeftBrace,
                    '}' => TokenKind.RightBrace,
                    '(' => TokenKind.LeftParen,
                    ')' => TokenKind.RightParen,
                    '[' => TokenKind.LeftBracket,
                    ']' => TokenKind.RightBracket,
                    ':' => TokenKind.Colon,
                    ',' => TokenKind.Comma,
                    '=' => TokenKind.Equals,
                    '@' => TokenKind.At,
                    _   => null
                };
                if (simple.HasValue) { tokens.Add(new Token(simple.Value, c.ToString(), _line)); _pos++; continue; }

                // String literal.
                if (c == '"' || c == '\'') { tokens.Add(ReadString(c)); continue; }

                // Number (including negative).
                if (char.IsDigit(c) || (c == '-' && _pos + 1 < _src.Length && char.IsDigit(_src[_pos + 1])))
                { tokens.Add(ReadNumber()); continue; }

                // Identifier / bool keyword.
                if (char.IsLetter(c) || c == '_') { tokens.Add(ReadIdentifierOrKeyword()); continue; }

                // Unknown character — skip to avoid infinite loops on bad input.
                // Also skips non-ASCII Unicode characters (box-drawing chars, etc.)
                // that may appear in decorative comments if the file was pasted from rich text.
                _pos++;
            }
            tokens.Add(new Token(TokenKind.EOF, string.Empty, _line));
            return tokens;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SkipWhitespaceAndComments()
        {
            while (_pos < _src.Length)
            {
                char c = _src[_pos];

                // Treat non-ASCII Unicode (box-drawing characters, smart quotes, etc.) as whitespace.
                if (c > 127) { _pos++; continue; }

                if (c == ' ' || c == '\t' || c == '\r') { _pos++; continue; }

                // Line comment: // or #
                if ((c == '/' && Peek(1) == '/') || c == '#')
                {
                    while (_pos < _src.Length && _src[_pos] != '\n') _pos++;
                    continue;
                }

                // Block comment: /* ... */
                if (c == '/' && Peek(1) == '*')
                {
                    _pos += 2;
                    while (_pos < _src.Length - 1 && !(_src[_pos] == '*' && _src[_pos + 1] == '/'))
                    {
                        if (_src[_pos] == '\n') _line++;
                        _pos++;
                    }
                    _pos += 2; // skip */
                    continue;
                }

                break;
            }
        }

        private char Peek(int offset) => (_pos + offset < _src.Length) ? _src[_pos + offset] : '\0';

        private Token ReadString(char delimiter)
        {
            _pos++; // skip opening quote
            var sb = new StringBuilder();
            while (_pos < _src.Length && _src[_pos] != delimiter)
            {
                if (_src[_pos] == '\\' && _pos + 1 < _src.Length)
                {
                    _pos++;
                    sb.Append(_src[_pos] switch { 'n' => '\n', 't' => '\t', _ => _src[_pos] });
                }
                else sb.Append(_src[_pos]);
                _pos++;
            }
            _pos++; // skip closing quote
            return new Token(TokenKind.String, sb.ToString(), _line);
        }

        private Token ReadNumber()
        {
            int start = _pos;
            if (_src[_pos] == '-') _pos++;
            while (_pos < _src.Length && (char.IsDigit(_src[_pos]) || _src[_pos] == '.')) _pos++;
            return new Token(TokenKind.Number, _src[start.._pos], _line);
        }

        private Token ReadIdentifierOrKeyword()
        {
            int start = _pos;
            while (_pos < _src.Length && (char.IsLetterOrDigit(_src[_pos]) || _src[_pos] == '_' || _src[_pos] == '.'))
                _pos++;
            string text = _src[start.._pos];
            return text.ToLowerInvariant() switch
            {
                "true"  => new Token(TokenKind.Bool, "true", _line),
                "false" => new Token(TokenKind.Bool, "false", _line),
                _       => new Token(TokenKind.Identifier, text, _line)
            };
        }
    }
}



