using System.Text.RegularExpressions;

namespace Statsify.Core.Expressions
{
    internal class NumberTokenScanner : ITokenScanner
    {
        private static Regex number = new Regex(@"\G[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?", RegexOptions.Compiled);

        public bool Scan(string source, int offset, TokenPosition position, out Token token, out string lexeme)
        {
            var m = number.Match(source, offset);
            if(!m.Success)
            {
                token = null;
                lexeme = null;

                return false;
            }

            var integer = 0L;
            lexeme = m.Value;
            
            token = 
                long.TryParse(lexeme, out integer) ? 
                    new Token(lexeme, TokenType.Integer) : 
                    new Token(lexeme, TokenType.Float);

            return true;
        }
    }
}