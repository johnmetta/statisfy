using System;
using System.Text.RegularExpressions;

namespace Statsify.Core.Expressions
{
    internal class RegexTokenScanner : ITokenScanner
    {
        public TokenType? TokenType { get; private set; }

        public Regex Regex { get; private set; }

        public Func<string, string> LexemePostprocessor { get; private set; }

        public RegexTokenScanner(TokenType? tokenType, Regex regex, Func<string, string> lexemePostprocessor)
        {
            TokenType = tokenType;
            Regex = regex;
            LexemePostprocessor = lexemePostprocessor;
        }

        public bool Scan(string source, int offset, TokenPosition position, out Token token, out string lexeme)
        {
            token = null;
            lexeme = null;
            
            var m = Regex.Match(source, offset);
            if(!m.Success)
                return false;

            lexeme = m.Value;

            //
            // Tell the ExpressionScanner to skip whatever @lexeme we have here
            if(!TokenType.HasValue)
                return true;

            token = new Token {
                //
                // Null check happens elsewhere.
                // ReSharper disable once PossibleInvalidOperationException
                Type = TokenType.Value,
                Lexeme = 
                    LexemePostprocessor == null ? 
                        lexeme :
                        LexemePostprocessor(lexeme),
                Position = position
            };

            return true;
        }
    }
}