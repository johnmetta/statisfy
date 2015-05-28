using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Statsify.Core.Model;

namespace Statsify.Core.Expressions
{
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
            var paramsPi = pis.SingleOrDefault(pi => pi.GetCustomAttributes(typeof(ParamArrayAttribute), false).OfType<ParamArrayAttribute>().SingleOrDefault() != null);
            var hasParams = paramsPi != null;
            var hasMetric = pis.All(pi => pi.ParameterType != typeof(MetricSelector));

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

                    var metricNames = environment.MetricRegistry.ResolveMetricNames(ms.Selector).ToArray();
                    var metrics = MetricReader.ReadMetrics(environment, metricNames, context.From, context.Until);

                    p[pos] = metrics.ToArray();
                } // if
            } // if

            return methodInfo.Invoke(null,  p.ToArray());
        }
    }
}