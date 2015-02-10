using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Statsify.Core.Expressions
{
    public class ExpressionParser
    {
        public Expression Parse(TokenStream tokens)
        {
            switch(tokens.Lookahead.Type)
            {
                case TokenType.Identifier:
                    var id = tokens.Read();
                    return tokens.Lookahead.Type == TokenType.OpenParen ? 
                        (Expression)ParseFunctionInvocationExpression(id, tokens) : 
                        ParseMetricSelectorExpression(id, tokens);
                case TokenType.String:
                    return new ConstantExpression(tokens.Read().Lexeme);
                case TokenType.Integer:
                    return new ConstantExpression(Convert.ToInt32(tokens.Read().Lexeme, CultureInfo.InvariantCulture));
                case TokenType.Float:
                    return new ConstantExpression(Convert.ToDouble(tokens.Read().Lexeme, CultureInfo.InvariantCulture));
                case TokenType.Keyword:
                    var token = tokens.Read();
                    var lexeme = token.Lexeme;

                    bool result;
                    if(bool.TryParse(lexeme, out result))
                        return new ConstantExpression(result);

                    throw new Exception(string.Format("unexpected '{0}' at {1}", token.Type, token.Position));
                default:
                    throw new Exception(string.Format("unexpected '{0}' at {1}", tokens.Lookahead.Type, tokens.Lookahead.Position));
            } // switch
        }

        private MetricSelectorExpression ParseMetricSelectorExpression(Token id, TokenStream tokens)
        {
            var selectorBuilder = new StringBuilder(id.Lexeme);
            var set = false;
            while(tokens.Any && (set || tokens.Lookahead.Type != TokenType.Comma) && tokens.Lookahead.Type != TokenType.CloseParen)
            {
                var token = tokens.Read();
                if(token.Type == TokenType.OpenCBrace)
                    set = true;
                else if(token.Type == TokenType.CloseCBrace)
                    set = false;
                
                selectorBuilder.Append(token.Lexeme);
            } // if

            return new MetricSelectorExpression(selectorBuilder.ToString());
        }

        private FunctionInvocationExpression ParseFunctionInvocationExpression(Token id, TokenStream tokens)
        {
            tokens.Read(TokenType.OpenParen);

            var arguments = new List<Argument>();
            while(tokens.Lookahead.Type != TokenType.CloseParen)
            {
                arguments.Add(new Argument(null, Parse(tokens)));
                if(tokens.Lookahead.Type != TokenType.CloseParen)
                    tokens.Read(TokenType.Comma);
            } // while

            tokens.Read(TokenType.CloseParen);

            return new FunctionInvocationExpression(id.Lexeme, arguments);
        }
    }
}
