using System;
using Statsify.Agent.Configuration;
using Statsify.Agent.Impl;

namespace Statsify.Tests.Agent.Impl
{
    internal class NullMetricDefinition : IMetricDefinition
    {
        public string Name { get; private set; }

        public AggregationStrategy AggregationStrategy { get; private set; }

        public double GetNextValue()
        {
            throw new NotImplementedException();
        }
    }
}