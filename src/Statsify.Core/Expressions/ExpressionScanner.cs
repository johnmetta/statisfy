using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Statsify.Core.Util;

namespace Statsify.Core.Expressions
{
    public class ExpressionScanner
    {
        private const int TabSize = 4;

        private readonly Regex keyword = new Regex(@"\G\b(library|define|module|node|and|case|default|else|elseif|false|true|if|in|import|include|inherits|or|unless|null|role)\b", RegexOptions.Compiled);
        private readonly Regex identifier = new Regex(@"\G[_a-zA-Z][_\-a-zA-Z0-9]*", RegexOptions.Compiled);
        private readonly Regex @string = new Regex(@"\G(""[^""\r\n\\]*(?:\\.[^""\r\n\\]*)*""|'[^'\r\n\\]*(?:\\.[^'\r\n\\]*)*')", RegexOptions.Compiled);
        private readonly Regex multilineString = new Regex(@"\G\""(?:[^""]|\"""")*\""", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly Regex integer = new Regex(@"\G\d+", RegexOptions.Compiled);
        private readonly Regex punctuation = new Regex(@"\G(\;|\:\:|\:|\.|,|=>|\{|\}|\[|\]|\(|\)|\$|\*|\?|\=|\@|\!)", RegexOptions.Compiled);
        private readonly Regex whitespace = new Regex(@"\G(\s|\r|\n)+", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly Regex comment = new Regex(@"\G#(.*?)\r?\n", RegexOptions.Compiled | RegexOptions.Multiline);

        private readonly IList<TokenScanner> tokenScanners = new List<TokenScanner>();

        public ExpressionScanner()
        {
            tokenScanners = new List<TokenScanner> {
                TokenScanner.Scan(TokenType.Keyword, keyword),
                TokenScanner.Scan(TokenType.Identifier, identifier),
                TokenScanner.Scan(TokenType.String, @string, s => s.Trim('\'').Trim('\"')),
                TokenScanner.Scan(TokenType.String, multilineString, s => s.Trim('\'').Trim('\"')),
                TokenScanner.Scan(TokenType.Integer, integer),

                TokenScanner.Scan(TokenType.OpenParen, @"\("),
                TokenScanner.Scan(TokenType.CloseParen, @"\)"),
                TokenScanner.Scan(TokenType.OpenBrace, @"\["),
                TokenScanner.Scan(TokenType.CloseBrace, @"\]"),
                TokenScanner.Scan(TokenType.OpenCBrace, @"\{"),
                TokenScanner.Scan(TokenType.CloseCBrace, @"\}"),
                TokenScanner.Scan(TokenType.Dot, @"\."),
                TokenScanner.Scan(TokenType.Asterisk, @"\*"),
                TokenScanner.Scan(TokenType.Comma, @"\,"),

                //TokenScanner.Scan(TokenType.Punctuation, punctuation),

                TokenScanner.Skip(whitespace),
                TokenScanner.Skip(comment),
            };
        }

        public IEnumerable<Token> Scan(string source)
        {
            var offset = 0;

            //
            // @line and @column are 1-based here.
            var line = 1;
            var column = 1;

            Token token;

            while(offset < source.Length)
            {
                var scanned = false;

                foreach(var scanner in tokenScanners)
                {
                    if(scanner.TokenType.HasValue)
                    {
                        // ReSharper disable once CSharpWarnings::CS0665
                        if(scanned = ScanToken(source, scanner, ref offset, ref line, ref column, out token))
                        {
                            yield return token;
                            break;
                        } // if
                    } //
                    else
                    {
                        // ReSharper disable once CSharpWarnings::CS0665
                        if(scanned = SkipToken(source, scanner.Regex, ref offset, ref line, ref column))
                            break;
                    } // else
                } // foreach

                if(!scanned)
                    throw new Exception(string.Format("unexpected '{0}' at ({1}, {2} ({3}))", source[offset], line, column, offset));
            } // while
        }

        private bool ScanToken(string source, TokenScanner scanner,ref int offset, ref int line, ref int column, out Token token)
        {
            var m = scanner.Regex.Match(source, offset);

            if(m.Success)
            {
                var lexeme = m.Value;
                if(scanner.LexemePostprocessor != null)
                    lexeme = scanner.LexemePostprocessor.Invoke(lexeme);

                token = new Token {
                    //
                    // Null check happens elsewhere.
                    // ReSharper disable once PossibleInvalidOperationException
                    Type = scanner.TokenType.Value, 
                    Lexeme = lexeme, 
                    Position = new TokenPosition(line, column)
                };

                AdvancePosition(m, ref offset, ref line, ref column);

                return true;
            } // if

            token = null;
            return false;
        }

        private bool SkipToken(string source, Regex regex, ref int offset, ref int line, ref int column)
        {
            var m = regex.Match(source, offset);
            if(m.Success)
                AdvancePosition(m, ref offset, ref line, ref column);

            return m.Success;
        }

        private void AdvancePosition(Match match, ref int offset, ref int line, ref int column)
        {
            offset += match.Value.Length;

            if(match.Value.Contains("\n"))
            {
                //
                // Multiline lexemes can span, well, multiple lines and can stretch very far to the right,
                // thus increasing initial @column value
                //
                // Aggregate() starts wiht 1 since @column is 1-based
                column = match.Value.SubstringAfterLast("\n").Trim('\r', '\n').Aggregate(1, (i, c) => i += c == '\t' ? TabSize : 1);
                line += match.Value.Count(c => c == '\n');
            }
            else
                column += match.Value.Length;
        }
    }
}