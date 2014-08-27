using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Statsify.Core.Model;

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
                    return new ConstantExpression(Convert.ToInt32(tokens.Read().Lexeme));
                default:
                    throw new Exception(string.Format("unexpected '{0}' at {1}", tokens.Lookahead.Type, tokens.Lookahead.Position));
            } // switch
        }

        private MetricSelectorExpression ParseMetricSelectorExpression(Token id, TokenStream tokens)
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

            return new MetricSelectorExpression(selectorBuilder.ToString());
        }

        private FunctionInvocationExpression ParseFunctionInvocationExpression(Token id, TokenStream tokens)
        {
            tokens.Read(TokenType.OpenParen);

            var parameters = new List<Expression>();
            while(tokens.Lookahead.Type != TokenType.CloseParen)
            {
                parameters.Add(Parse(tokens));
                if(tokens.Lookahead.Type != TokenType.CloseParen)
                    tokens.Read(TokenType.Comma);
            } // while

            tokens.Read(TokenType.CloseParen);

            return new FunctionInvocationExpression(id.Lexeme, parameters);
        }
    }

    public class Function
    {
        private readonly MethodInfo methodInfo;

        public Function(MethodInfo methodInfo)
        {
            this.methodInfo = methodInfo;
        }

        public object Invoke(Environment environment, EvalContext context, object[] parameters)
        {
            var p = new List<object> { context };

            var pis = methodInfo.GetParameters();
            var paramsPi = pis.SingleOrDefault(pi => pi.GetCustomAttribute<ParamArrayAttribute>() != null);
            var hasParams = paramsPi != null;
            var hasMetric = pis.All(pi => pi.GetType() != typeof(MetricSelector));

            //
            // First parameter must always be an EvalContext instance
            if(hasParams)
            {
                p.AddRange(parameters.Take(pis.Length - 2));
                var @params = parameters.Skip(pis.Length - 2).ToArray();

                var par = Array.CreateInstance(paramsPi.ParameterType.GetElementType(), @params.Length);
                Array.Copy(@params, par, @params.Length);

                p.Add(par);
            } // if
            else
                p.AddRange(parameters);

            if(hasMetric)
            {
                var pos = p.FindIndex(_p => _p is MetricSelector);
                if(pos > -1)
                {
                    var ms = p[pos] as MetricSelector;

                    var metrics = new List<Metric>();
                    foreach(var metricName in environment.MetricNameResolver.ResolveMetricNames(ms.Selector))
                    {
                        var metric = 
                            environment.MetricReader.ReadMetric(metricName, context.From, context.Until);

                        metrics.Add(metric);
                    } // foreach

                    p[pos] = metrics.ToArray();
                } // if
            } // if

            return methodInfo.Invoke(null,  p.ToArray());
        }
    }
}
