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
            if(metric.Type == "rabbitmq-queues")
            {
                var metricSource = CreateRabbitMqQueuesMetricSource(metric);
                return metricSource;
            } // if

            var metricDefinitions = metricDefinitionFactory.CreateMetricDefinitions(metric);
            
            if(metric.RefreshEvery.HasValue)
                return new RefreshableMetricSource(metricDefinitions, metric.RefreshEvery.Value, () => metricDefinitionFactory.CreateMetricDefinitions(metric));

            return new MetricSource(metricDefinitions);
        }

        private IMetricSource CreateRabbitMqQueuesMetricSource(MetricConfigurationElement metric)
        {
            return new RabbitMqQueuesMetricSource(metric);
        }
    }
}
