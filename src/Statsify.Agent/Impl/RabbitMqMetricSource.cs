using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Statsify.Agent.Configuration;
using Statsify.Client;

namespace Statsify.Agent.Impl
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <see cref="IMetricConfiguration.Path"/> must contain an absolute URL of RabbitMQ installation, possibly with
    /// credentials embedded (<c>http://login:password@rabbitmq.local:15672</c>).
    /// </remarks>
    public class RabbitMqMetricSource : IMetricSource
    {
        private readonly AggregationStrategy aggregationStrategy;
        private readonly string name;
        private readonly DataContractJsonSerializer jsonSerializer;
        private readonly Uri uri;
        private readonly NetworkCredential credentials;

        public RabbitMqMetricSource(IMetricConfiguration metric)
        {
            name = metric.Name;
            aggregationStrategy = metric.AggregationStrategy;

            uri = new Uri(Environment.ExpandEnvironmentVariables(metric.Path));
            RewriteUrl(ref uri, out credentials);

            jsonSerializer = new DataContractJsonSerializer(typeof(Overview));
        }

        public IEnumerable<IMetricDefinition> GetMetricDefinitions()
        {
            byte[] buffer;
            using(var webClient = new WebClient())
            {
                if(credentials != null)
                    webClient.Credentials = credentials;

                buffer = webClient.DownloadData(uri);
            } // using

            Overview overview;

            using(var memoryStream = new MemoryStream(buffer))
                overview = (Overview)jsonSerializer.ReadObject(memoryStream);

            Func<string, Func<double>, MetricDefinition> metricDefinition = 
                (s, vp) => new MetricDefinition(MetricNameBuilder.BuildMetricName(name, s), vp, aggregationStrategy);
                
            yield return metricDefinition("message_stats.ack", () => overview.MessageStats.Ack);
            yield return metricDefinition("message_stats.ack_details.rate", () => overview.MessageStats.AckDetails.Rate);
            
            yield return metricDefinition("message_stats.deliver", () => overview.MessageStats.Deliver);
            yield return metricDefinition("message_stats.deliver_details.rate", () => overview.MessageStats.DeliverDetails.Rate);

            yield return metricDefinition("message_stats.redeliver", () => overview.MessageStats.Redeliver);
            yield return metricDefinition("message_stats.redeliver_details.rate", () => overview.MessageStats.RedeliverDetails.Rate);

            yield return metricDefinition("queue_totals.messages", () => overview.QueueTotals.Messages);
            yield return metricDefinition("queue_totals.messages_details.rate", () => overview.QueueTotals.MessagesDetails.Rate);

            yield return metricDefinition("queue_totals.messages_ready", () => overview.QueueTotals.MessagesReady);
            yield return metricDefinition("queue_totals.messages_ready_details.rate", () => overview.QueueTotals.MessagesReadyDetails.Rate);

            yield return metricDefinition("queue_totals.messages_unacknowledged", () => overview.QueueTotals.MessagesUnacknowledged);
            yield return metricDefinition("queue_totals.messages_unacknowledged_details.rate", () => overview.QueueTotals.MessagesUnacknowledgedDetails.Rate);

            yield return metricDefinition("object_totals.consumers", () => overview.ObjectTotals.Consumers);
            yield return metricDefinition("object_totals.queues", () => overview.ObjectTotals.Queues);
            yield return metricDefinition("object_totals.exchanges", () => overview.ObjectTotals.Exchanges);
            yield return metricDefinition("object_totals.connections", () => overview.ObjectTotals.Connections);
            yield return metricDefinition("object_totals.channels", () => overview.ObjectTotals.Channels);
        }

        public void InvalidateMetricDefinition(IMetricDefinition metricDefinition)
        {
        }

        internal static void RewriteUrl(ref Uri uri, out NetworkCredential credentials)
        {
            var uriBuilder = new UriBuilder(uri);
            if(string.IsNullOrWhiteSpace(uriBuilder.UserName) || string.IsNullOrWhiteSpace(uriBuilder.Password))
            {
                credentials = null;
                return;
            } // if

            credentials = new NetworkCredential(
                Uri.UnescapeDataString(uriBuilder.UserName), 
                Uri.UnescapeDataString(uriBuilder.Password));

            uriBuilder.UserName = "";
            uriBuilder.Password = "";

            uriBuilder.Path = uriBuilder.Path.TrimEnd('/') + "/api/overview";

            uri = new Uri(uriBuilder.ToString());
        }

        [DataContract]
        public class Overview
        {
            [DataMember(Name = "message_stats")]
            public MessageStats MessageStats { get; set; }

            [DataMember(Name = "queue_totals")]
            public QueueTotals QueueTotals { get; set; }

            [DataMember(Name = "object_totals")]
            public ObjectTotals ObjectTotals { get; set; }
        }

        [DataContract]
        public class MessageStats
        {
            [DataMember(Name = "ack")]
            public int Ack { get; set; }

            [DataMember(Name = "ack_details")]
            public StatsDetails AckDetails { get; set; }

            [DataMember(Name = "deliver")]
            public int Deliver { get; set; }

            [DataMember(Name = "deliver_details")]
            public StatsDetails DeliverDetails { get; set; }

            [DataMember(Name = "redeliver")]
            public int Redeliver { get; set; }

            [DataMember(Name = "redeliver_details")]
            public StatsDetails RedeliverDetails { get; set; }
        }

        [DataContract]
        public class StatsDetails
        {
            [DataMember(Name = "rate")]
            public float Rate { get; set; }

            [DataMember(Name = "interval")]
            public float Interval { get; set; } // FIXME: Or is it int?

            [DataMember(Name = "last_event")]
            public long LastEvent { get; set; }
        }

        [DataContract]
        public class QueueTotals
        {
            [DataMember(Name = "messages")]
            public int Messages { get; set; }

            [DataMember(Name =  "messages_details")]
            public StatsDetails MessagesDetails { get; set; }

            [DataMember(Name = "messages_ready")]
            public int MessagesReady { get; set; }

            [DataMember(Name =  "messages_ready_details")]
            public StatsDetails MessagesReadyDetails { get; set; }

            [DataMember(Name = "messages_unacknowledged")]
            public int MessagesUnacknowledged { get; set; }

            [DataMember(Name =  "messages_unacknowledged_details")]
            public StatsDetails MessagesUnacknowledgedDetails { get; set; }
        }

        [DataContract]
        public class ObjectTotals
        {
            [DataMember(Name = "consumers")]
            public int Consumers { get; set; }

            [DataMember(Name = "queues")]
            public int Queues { get; set; }

            [DataMember(Name = "exchanges")]
            public int Exchanges { get; set; }

            [DataMember(Name = "connections")]
            public int Connections { get; set; }

            [DataMember(Name = "channels")]
            public int Channels { get; set; }
        }
    }
}