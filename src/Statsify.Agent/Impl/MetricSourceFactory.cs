using Statsify.Agent.Configuration;

namespace Statsify.Agent.Impl
{
    public class MetricSourceFactory
    {
        private readonly MetricDefinitionFactory metricDefinitionFactory;

        public MetricSourceFactory(MetricDefinitionFactory metricDefinitionFactory)
        {
            this.metricDefinitionFactory = metricDefinitionFactory;
        }

        public IMetricSource CreateMetricSource(MetricConfigurationElement metric)
        {
            if(metric.Type.StartsWith("rabbitmq-"))
            {
                var metricSource = new RabbitMqMetricSource(metric);
                return metricSource;
            } // if

            var metricDefinitions = metricDefinitionFactory.CreateMetricDefinitions(metric);
            
            if(metric.RefreshEvery.HasValue)
                return new RefreshableMetricSource(metricDefinitions, metric.RefreshEvery.Value, () => metricDefinitionFactory.CreateMetricDefinitions(metric));

            return new MetricSource(metricDefinitions);
        }
    }
}
