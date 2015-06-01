using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NLog;
using Statsify.Agent.Configuration;
using Statsify.Agent.Util;

namespace Statsify.Agent.Impl
{
    public class MetricDefinitionFactory
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public IEnumerable<MetricDefinition> CreateMetricDefinitions(MetricConfigurationElement metric)
        {
            var type = metric.Type.ToLowerInvariant();
            return CreateMetricDefinitions(metric, type);
        }

        private IEnumerable<MetricDefinition> CreateMetricDefinitions(MetricConfigurationElement metric, string type)
        {
            switch(type)
            {
                case "performance-counter":
                    return CreatePerformanceCounterMetricDefinition(metric);
                case "number-of-files":
                    return CreateNumberOfFilesDefinition(metric);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        private IEnumerable<MetricDefinition> CreateNumberOfFilesDefinition(MetricConfigurationElement metric)
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

        private IEnumerable<MetricDefinition> CreatePerformanceCounterMetricDefinition(MetricConfigurationElement metric)
        {
            return ParsePerformanceCounters(metric.Path).Select(pc => new MetricDefinition(metric.Name, () => pc.NextValue(), metric.AggregationStrategy));
        }

        public static IEnumerable<PerformanceCounter> ParsePerformanceCounters(string s)
        {
            string machineName;
            string categoryName;
            string instanceName;
            string counterName;

            ParsePerformanceCounterDefinition(s, out machineName, out categoryName, out instanceName, out counterName);
            
            var performanceCounter = CreatePerformanceCounter(machineName, categoryName, instanceName, counterName);
            yield return performanceCounter;
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
    }
}