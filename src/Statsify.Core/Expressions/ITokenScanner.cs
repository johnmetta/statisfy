namespace Statsify.Core.Expressions
{
    internal interface ITokenScanner
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="offset"></param>
        /// <param name="position"></param>
        /// <param name="token"></param>
        /// <param name="lexeme">The lexeme parsed. This can differ from <see cref="Token.Lexeme"/>.</param>
        /// <returns></returns>
        bool Scan(string source, int offset, TokenPosition position, out Token token, out string lexeme);
    }
}