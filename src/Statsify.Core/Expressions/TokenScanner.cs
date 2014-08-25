using System;
using System.Text.RegularExpressions;

namespace Statsify.Core.Expressions
{
    internal class TokenScanner
    {
        public TokenType? TokenType { get; private set; }

        public Regex Regex { get; private set; }

        public Func<string, string> LexemePostprocessor { get; private set; } 

        public static TokenScanner Scan(TokenType tokenType, string regex, RegexOptions options = RegexOptions.None, Func<string, string> lexemePostprocessor = null)
        {
            return Scan(tokenType, new Regex(@"\G" + regex, RegexOptions.Compiled | options), lexemePostprocessor);
        }

        public static TokenScanner Scan(TokenType tokenType, Regex regex, Func<string, string> lexemePostprocessor = null)
        {
            return new TokenScanner { TokenType = tokenType, Regex = regex, LexemePostprocessor = lexemePostprocessor };
        }

        public static TokenScanner Skip(Regex regex)
        {
            return new TokenScanner { Regex = regex };
        }
    }
}