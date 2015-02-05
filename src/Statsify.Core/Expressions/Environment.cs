using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Statsify.Core.Components;

namespace Statsify.Core.Expressions
{
    public class Environment
    {
        private static readonly IDictionary<string, Function> Functions = new Dictionary<string, Function>();

        public IMetricRegistry MetricRegistry { get; set; }

        public static void RegisterFunction(string name, Function function)
        {
            Functions[name] = function;
        }

        public static void RegisterFunctions(Type type)
        {
            foreach(var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                foreach(var functionAttribute in methodInfo.GetCustomAttributes<FunctionAttribute>())
                {
                    RegisterFunction(functionAttribute.Name, new Function(methodInfo));
                } // foreach
            } // foreach
        }

        public Function ResolveFunction(string name)
        {
            return Functions[name];
        }
    }
}