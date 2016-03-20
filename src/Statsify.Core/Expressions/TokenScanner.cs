using System;
using System.Text.RegularExpressions;

namespace Statsify.Core.Expressions
{
    internal static class TokenScanner
    {
        public static ITokenScanner Scan(TokenType tokenType, string regex, RegexOptions options = RegexOptions.None, Func<string, string> lexemePostprocessor = null)
        {
            if(!regex.StartsWith(@"\G"))
                regex = @"\G" + regex;

            return Scan(tokenType, new Regex(regex, RegexOptions.Compiled | options), lexemePostprocessor);
        }

        public static ITokenScanner Scan(TokenType tokenType, Regex regex, Func<string, string> lexemePostprocessor = null)
        {
            return new RegexTokenScanner(tokenType, regex, lexemePostprocessor);
        }

        public static RegexTokenScanner Skip(Regex regex)
        {
            return new RegexTokenScanner(null, regex, null);
        }
    }
}