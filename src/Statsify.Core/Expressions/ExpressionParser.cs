using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

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
                    if(tokens.Lookahead.Type == TokenType.OpenParen)
                        return ParseFunctionInvocationExpression(id, tokens);
                    else
                        return ParseSeriesSelector(id, tokens);
                    break;
                case TokenType.String:
                    return new ConstantExpression(tokens.Read().Lexeme);
                    break;
                case TokenType.Integer:
                    return new ConstantExpression(Convert.ToInt32(tokens.Read().Lexeme));
                    break;
                default:
                    throw new Exception(string.Format("unexpected '{0}' at {1}", tokens.Lookahead.Type, tokens.Lookahead.Position));
            } // switch

            return null;
        }

        private SeriesSelectorExpression ParseSeriesSelector(Token id, TokenStream tokens)
        {
            var selectorBuilder = new StringBuilder(id.Lexeme);
            var set = false;
            while((set || tokens.Lookahead.Type != TokenType.Comma) && tokens.Lookahead.Type != TokenType.CloseParen)
            {
                var token = tokens.Read();
                if(token.Type == TokenType.OpenCBrace)
                    set = true;
                else if(token.Type == TokenType.CloseCBrace)
                    set = false;
                
                selectorBuilder.Append(token.Lexeme);
            } // if

            return new SeriesSelectorExpression(selectorBuilder.ToString());
        }

        private Expression ParseFunctionInvocationExpression(Token id, TokenStream tokens)
        {
            tokens.Read(TokenType.OpenParen);

            var parameters = new List<Expression>();
            while(tokens.Lookahead.Type != TokenType.CloseParen)
            {
                parameters.Add(Parse(tokens));
                if(tokens.Lookahead.Type != TokenType.CloseParen)
                    tokens.Read(TokenType.Comma);
            } // while

            return new FunctionInvocationExpression(id.Lexeme, parameters);
        }
    }

    [DebuggerDisplay("{Selector,nq}")]
    internal class SeriesSelectorExpression : Expression
    {
        public string Selector { get; private set; }

        public SeriesSelectorExpression(string selector)
        {
            Selector = selector;
        }
    }

    public abstract class Expression
    {
        public virtual object Evaluate(Environment environment)
        {
            return null;
        }
    }

    [DebuggerDisplay("{Value,nq}")]
    public class ConstantExpression : Expression
    {
        public object Value { get; private set; }

        public ConstantExpression(object value)
        {
            Value = value;
        }

        public override object Evaluate(Environment environment)
        {
            return Value;
        }
    }

    [DebuggerDisplay("{Name,nq}()")]
    public class FunctionInvocationExpression : Expression
    {
        public string Name { get; private set; }

        public ReadOnlyCollection<Expression> Parameters { get; private set; }

        public FunctionInvocationExpression(string name, IEnumerable<Expression> parameters)
        {
            Name = name;
            Parameters = new ReadOnlyCollection<Expression>(new List<Expression>(parameters));
        }

        public override object Evaluate(Environment environment)
        {
            var parameters = Parameters.Select(p => p.Evaluate(environment)).ToArray();
            var function = environment.ResolveFunction(Name);

            return function.Invoke(parameters);
        }
    }

    public class Environment
    {
        public Function ResolveFunction(string name)
        {
            return null;
        }
    }

    public class Function
    {
        public object Invoke(object[] parameters)
        {
            return null;
        }
    }
}
