using System.Collections.Generic;
using Statsify.Core.Components;

namespace Statsify.Core.Expressions
{
    public class Environment
    {
        private static readonly IDictionary<string, Function> Functions = new Dictionary<string, Function>();

        public IMetricReader MetricReader { get; set; }
        public IMetricNameResolver MetricNameResolver { get; set; }

        public static void RegisterFunction(string name, Function function)
        {
            Functions[name] = function;
        }

        public Function ResolveFunction(string name)
        {
            return Functions[name];
        }
    }
}