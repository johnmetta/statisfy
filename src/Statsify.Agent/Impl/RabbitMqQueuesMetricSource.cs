using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Statsify.Agent.Configuration;
using Statsify.Client;

namespace Statsify.Agent.Impl
{
    public class RabbitMqQueuesMetricSource : IMetricSource
    {
        private readonly AggregationStrategy aggregationStrategy;
        private readonly string @namespace;
        private readonly WebClient webClient;
        private readonly DataContractJsonSerializer jsonSerializer;
        private readonly Uri uri;

        public RabbitMqQueuesMetricSource(MetricConfigurationElement metric)
        {
            @namespace = metric.Name;
            aggregationStrategy = metric.AggregationStrategy;

            uri = new Uri(metric.Path);
            var credentials = GetCredentials(ref uri);

            webClient = new WebClient();

            if(credentials != null)
                webClient.Credentials = credentials;

            jsonSerializer = new DataContractJsonSerializer(typeof(Queue[]));
        }

        public IEnumerable<IMetricDefinition> GetMetricDefinitions()
        {
            var buffer = webClient.DownloadData(uri);
            Queue[] queues;

            using(var memoryStream = new MemoryStream(buffer))
                queues = (Queue[])jsonSerializer.ReadObject(memoryStream);

            var metricDefinitions = 
                queues.
                    Select(q =>
                    {
                        var name = MetricNameBuilder.BuildMetricName(@namespace, MetricNameBuilder.BuildMetricName(q.VirtualHost, q.Name));
                        var metricDefinition = new MetricDefinition(name, () => q.BackingQueueStatus.RamMessagesCount, aggregationStrategy);

                        return metricDefinition;
                    }).
                    ToArray();

            return metricDefinitions;
        }

        public void InvalidateMetricDefinition(IMetricDefinition metricDefinition)
        {
        }

        private ICredentials GetCredentials(ref Uri uri)
        {
            var uriBuilder = new UriBuilder(uri);
            if(string.IsNullOrWhiteSpace(uriBuilder.UserName) || string.IsNullOrWhiteSpace(uriBuilder.Password))
                return null;

            var credentials = new NetworkCredential(uriBuilder.UserName, uriBuilder.Password);
            uriBuilder.UserName = "";
            uriBuilder.Password = "";

            uri = new Uri(uriBuilder.ToString());
            
            return credentials;
        }

        [DataContract]
        public class Queue
        {
            [DataContract]
            public class QueueStatus
            {
                [DataMember(Name = "ram_msg_count")]
                public int RamMessagesCount { get; set; }
            }

            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "vhost")]
            public string VirtualHost { get; set; }

            [DataMember(Name = "backing_queue_status")]
            public QueueStatus BackingQueueStatus { get; set; }
        }
    }
}