using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using NLog;
using Statsify.Agent.Configuration;
using Statsify.Agent.Util;
using Statsify.Client;

namespace Statsify.Agent.Impl
{
    public class MetricDefinitionFactory
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public IEnumerable<IMetricDefinition> CreateMetricDefinitions(MetricConfigurationElement metric)
        {
            var type = metric.Type.ToLowerInvariant();
            return CreateMetricDefinitions(metric, type);
        }

        private IEnumerable<IMetricDefinition> CreateMetricDefinitions(MetricConfigurationElement metric, string type)
        {
            switch(type)
            {
                case "performance-counter":
                    return CreatePerformanceCounterMetricDefinition(metric, s => s);
                case "iis-performance-counter":
                    //
                    // IIS performance counters are prefixed with "PID_", which
                    // we need to get rid of
                    return CreatePerformanceCounterMetricDefinition(metric, s => s.SubstringAfter("_"));
                case "wcf-performance-counter":
                    //
                    // WCF performance counters are prefixed with "ServiceName@" and contain
                    // pipe-delimited crap like Service@http:||foo:8080|bar
                    return CreatePerformanceCounterMetricDefinition(metric, NormalizeWcfPerformanceCounterName);
                case "number-of-files":
                    return CreateNumberOfFilesDefinition(metric);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        private IEnumerable<IMetricDefinition> CreateNumberOfFilesDefinition(MetricConfigurationElement metric)
        {
            var path = metric.Path;
            if(!Directory.Exists(path)) yield break;

            yield return new MetricDefinition(
                metric.Name, 
                () => {
                    var numberOfFiles = new DirectoryInfo(path).EnumerateFiles("*.*", SearchOption.TopDirectoryOnly).Count();
                    return numberOfFiles;
                }, metric.AggregationStrategy);
        }

        private IEnumerable<IMetricDefinition> CreatePerformanceCounterMetricDefinition(MetricConfigurationElement metric, 
            Func<string, string> metricNameProcessor)
        {
            return ParsePerformanceCounters(metric.Path).Select(pc => {
                var name = metric.Name;

                if(!string.IsNullOrWhiteSpace(pc.Item1) && name.Contains("**"))
                {
                    var fragment = MetricNameBuilder.SanitizeMetricName(metricNameProcessor(pc.Item1)).ToLowerInvariant();
                    
                    fragment = fragment.Trim('_');
                    while(fragment.Contains("__"))
                        fragment = fragment.Replace("__", "_");

                    fragment = fragment.Replace(".", "_");

                    name = name.Replace("**", fragment);
                } // if

                var metricDefinition = new PerformanceCounterMetricDefinition(name, () => pc.Item2.NextValue(), metric.AggregationStrategy);
                return metricDefinition;
            });
        }

        public static IEnumerable<Tuple<string, PerformanceCounter>> ParsePerformanceCounters(string s)
        {
            string machineName;
            string categoryName;
            string instanceName;
            string counterName;

            ParsePerformanceCounterDefinition(s, out machineName, out categoryName, out instanceName, out counterName);

            //
            // See #1: Add support for expanding multi-instance performance counters into distinct metrics
            IList<string> instanceNames = new List<string>();
            if(instanceName == "**")
            {
                var cc = Thread.CurrentThread.CurrentCulture;
                var cuic = Thread.CurrentThread.CurrentUICulture;

                //
                // Otherwise PerformanceCounterCategory.GetCategories() will return weirdo localized names
                Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

                var performanceCounterCategory = 
                    PerformanceCounterCategory.GetCategories().
                        FirstOrDefault(c => c.CategoryName == categoryName);
                if(performanceCounterCategory == null)
                {
                    Log.Warn("unknown Performance Counter Category '{0}'", categoryName);
                    yield break;
                } // if

                instanceNames = performanceCounterCategory.GetInstanceNames().ToArray();

                Thread.CurrentThread.CurrentCulture = cc;
                Thread.CurrentThread.CurrentUICulture = cuic;

                if(instanceName.Length == 0)
                    instanceNames.Add("");
            } // if
            else
                instanceNames.Add(instanceName ?? "");

            foreach(var t in instanceNames.Select(n => Tuple.Create(n, CreatePerformanceCounter(machineName, categoryName, n, counterName))).Where(n => n.Item2 != null))
                yield return t;
        }

        private static PerformanceCounter CreatePerformanceCounter(string machineName, string categoryName, string instanceName, string counterName)
        {
            var performanceCounter = new PerformanceCounter();
            if(!string.IsNullOrWhiteSpace(machineName))
                performanceCounter.MachineName = machineName;

            performanceCounter.CategoryName = categoryName;

            if(!string.IsNullOrWhiteSpace(instanceName))
                performanceCounter.InstanceName = instanceName;

            performanceCounter.CounterName = counterName;

            try
            {
                performanceCounter.NextValue();
            } // try
            catch(Exception e)
            {
                Log.ErrorException(string.Format(@"could not initialize Performance Counter from '{0}\{1}\{2}'", 
                    string.IsNullOrWhiteSpace(machineName) ? "" : @"\\" + machineName,
                    categoryName + (string.IsNullOrWhiteSpace(instanceName) ? "" : "(" + instanceName + ")"),
                    counterName), e);

                return null;
            } // catch

            return performanceCounter;
        }

        public static void ParsePerformanceCounterDefinition(string s, out string machineName, out string categoryName,
            out string instanceName, out string counterName)
        {
            var fragments = new Queue<string>(s.Trim('\\').Split('\\'));

            machineName = s.StartsWith(@"\\") && fragments.Count == 3 ? 
                fragments.Dequeue() : 
                null;
 
            categoryName = fragments.Dequeue();
            
            instanceName = 
                categoryName.Contains("(") && categoryName.EndsWith(")") ? 
                    categoryName.SubstringBetween("(", ")") : 
                    null;
            
            if(!string.IsNullOrWhiteSpace(instanceName))
                categoryName = categoryName.SubstringBefore("(");

            counterName = fragments.Dequeue();
        }

        internal static string NormalizeWcfPerformanceCounterName(string counterName)
        {
            //
            // See this:
            // * https://msdn.microsoft.com/en-us/library/ms731087(v=vs.110).aspx
            // * https://msdn.microsoft.com/en-us/library/ms735098.aspx
            // * http://stackoverflow.com/q/41393811/60188
            var serviceName = counterName.SubstringBefore("@").Replace('.', '_');
            var serviceBaseAddress = counterName.SubstringAfter("@").Trim('|').Replace('.', '_');

            var normalizedCounterName = string.Format("{0}.{1}", 
                MetricNameBuilder.SanitizeMetricName(serviceName).ToLowerInvariant(),
                MetricNameBuilder.SanitizeMetricName(serviceBaseAddress).ToLowerInvariant());

            while(normalizedCounterName.Contains("__"))
                normalizedCounterName = normalizedCounterName.Replace("__", "_");

            return normalizedCounterName;
        }
    }
}