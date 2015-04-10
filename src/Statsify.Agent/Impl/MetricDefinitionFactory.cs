using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using Statsify.Agent.Configuration;

namespace Statsify.Agent.Impl
{
    public class MetricDefinitionFactory
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly Regex PerformanceCounterParser =  new Regex(@"(\\\\(?<computer>([^\\]+)))?(\\(?<object>([^\\]+)))\\(?<counter>(.+))", RegexOptions.Compiled | RegexOptions.Singleline);

        public MetricDefinition CreateInstance(MetricConfigurationElement metric)
        {
            var type = metric.Type.ToLowerInvariant();
            return CreateInstance(metric, type);
        }

        private MetricDefinition CreateInstance(MetricConfigurationElement metric, string type)
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

        private MetricDefinition CreateNumberOfFilesDefinition(MetricConfigurationElement metric)
        {
            var path = metric.Path;
            if(!Directory.Exists(path)) return null;

            return new MetricDefinition(
                metric.Name, 
                () => {
                    var numberOfFiles = new DirectoryInfo(path).EnumerateFiles("*.*", SearchOption.TopDirectoryOnly).Count();
                    return numberOfFiles;
                }, metric.AggregationStrategy);
        }

        private MetricDefinition CreatePerformanceCounterMetricDefinition(MetricConfigurationElement metric)
        {
            var performanceCounter = ParsePerformanceCounter(metric.Path);
            return performanceCounter == null ?
                null :
                new MetricDefinition(metric.Name, () => performanceCounter.NextValue(), metric.AggregationStrategy);
        }

        private PerformanceCounter ParsePerformanceCounter(string s)
        {
            var performanceCounter = new PerformanceCounter();

            var match = PerformanceCounterParser.Match(s);

            var machineName = match.Groups["computer"].Value;

            if(!string.IsNullOrWhiteSpace(machineName))
                performanceCounter.MachineName = machineName;

            var category = match.Groups["object"].Value;

            if(category.Contains("("))
            {
                var ix = category.IndexOf("(", StringComparison.Ordinal);

                var categoryName = category.Substring(0, ix).Trim();

                performanceCounter.CategoryName = categoryName;
                performanceCounter.InstanceName = category.Substring(ix).Trim('(', ')');
            }
            else
            {
                performanceCounter.CategoryName = category;
            }

            performanceCounter.CounterName = match.Groups["counter"].Value;

            try
            {
                performanceCounter.NextValue();
            }
            catch (Exception e)
            {
                Log.ErrorException(string.Format("could not create performance counter from '{0}'", s), e);
                return null;
            }

            return performanceCounter;
        }

    }
}