using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
                    if(tokens.Lookahead.Type == TokenType.OpenParen)
                        return ParseFunctionInvocationExpression(id, tokens);
                    else
                        return ParseMetricSelectorExpression(id, tokens);
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

            return new FunctionInvocationExpression(id.Lexeme, parameters);
        }
    }

    [DebuggerDisplay("{Selector,nq}")]
    internal class MetricSelectorExpression : Expression
    {
        public string Selector { get; private set; }

        public MetricSelectorExpression(string selector)
        {
            Selector = selector;
        }

        public override object Evaluate(Environment environment, EvalContext context)
        {
            return new MetricSelector(Selector, context.From, context.Until);
        }
    }

    public abstract class Expression
    {
        public virtual object Evaluate(Environment environment, EvalContext context)
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

        public override object Evaluate(Environment environment, EvalContext context)
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

        public override object Evaluate(Environment environment, EvalContext context)
        {
            var parameters = Parameters.Select(p => p.Evaluate(environment, context)).ToArray();
            var function = environment.ResolveFunction(Name);

            return function.Invoke(environment, context, parameters);
        }
    }

    public class Environment
    {
        private static readonly IDictionary<string, Function> Functions = new Dictionary<string, Function>();

        public ISeriesReader SeriesReader { get; set; }
        public IMetricProvider MetricProvider { get; set; }

        public static void RegisterFunction(string name, Function function)
        {
            Functions[name] = function;
        }

        public Function ResolveFunction(string name)
        {
            return Functions[name];
        }
    }

    public interface IMetricProvider
    {
        IEnumerable<string> GetMetricNames(string selector);
    }

    public interface ISeriesReader
    {
        Series ReadSeries(string metric, DateTime from, DateTime until, TimeSpan? precision = null);
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
            var hasParams = pis.Any(pi => pi.GetCustomAttribute<ParamArrayAttribute>() != null);
            var hasMetric = pis.All(pi => pi.GetType() != typeof(MetricSelector));

            //
            // First parameter must always be an EvalContext instance
            if(hasParams)
            {
                p.AddRange(parameters.Take(pis.Length - 2));
                var @params = parameters.Skip(pis.Length - 2).ToArray();

                p.Add(@params);

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
                    foreach(var metricName in environment.MetricProvider.GetMetricNames(ms.Selector))
                    {
                        var series = environment.SeriesReader.ReadSeries(metricName, context.From, context.Until);
                        var metric = new Metric(metricName, series);

                        metrics.Add(metric);
                    } // foreach

                    p[pos] = metrics.ToArray();
                } // if
            } // if

            return methodInfo.Invoke(null,  p.ToArray());
        }
    }
}
