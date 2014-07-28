﻿using System;
using System.Diagnostics;
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
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
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
                Log.Error("could not create performance counter: {0}", e.Message);
                return null;
            }

            return performanceCounter;
        }

    }
}