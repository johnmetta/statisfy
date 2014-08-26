using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Statsify.Core.Model;
using Statsify.Core.Storage;

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
        IEnumerable<string> GetMetricNames(string metricNameSelector);
    }

    public interface ISeriesReader
    {
        Series ReadSeries(string metric, DateTime from, DateTime until, TimeSpan? precision = null);
    }

    public class X : IMetricProvider, ISeriesReader
    {
        private readonly string rootDirectory;

        public X(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
        }

        public IEnumerable<string> GetMetricNames(string metricNameSelector)
        {
            return GetDatabaseFilePaths(metricNameSelector).
                Select(f => {
                    var directoryName = Path.GetDirectoryName(f.FullName);
                    Debug.Assert(directoryName != null, "directoryName != null");
                    directoryName = directoryName.Substring(rootDirectory.Length + 1);
                    
                    var fileName = Path.GetFileNameWithoutExtension(f.FullName);
                    
                    return Path.Combine(directoryName, fileName).Replace(Path.DirectorySeparatorChar, '.');
                });
        }

        private IEnumerable<FileInfo> GetDatabaseFilePaths(string metricNameSelector)
        {
            var fragments = metricNameSelector.Split('.');
            return GetDatabaseFilePaths(new DirectoryInfo(rootDirectory), fragments, 0);
        }

        private IEnumerable<FileInfo> GetDatabaseFilePaths(DirectoryInfo directory, string[] fragments, int i)
        {
            if(i == fragments.Length - 1)
            {
                var files = directory.GetFiles(fragments[i] + ".db");
                foreach(var file in files)
                    yield return file;
            } // if
            else
            {
                foreach(var subdirectory in directory.GetDirectories(fragments[i]))
                {
                    directory = new DirectoryInfo(Path.Combine(directory.FullName, subdirectory.Name));
                    foreach(var metricName in GetDatabaseFilePaths(directory, fragments, i + 1))
                        yield return metricName;
                } // foreach
            } // else
        }

        public Series ReadSeries(string metric, DateTime @from, DateTime until, TimeSpan? precision = null)
        {
            var databaseFilePath = GetDatabaseFilePaths(metric).FirstOrDefault();
            if(databaseFilePath == null) return null;

            var database = DatapointDatabase.Open(databaseFilePath.FullName);
            return database.ReadSeries(from, until, precision);
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
