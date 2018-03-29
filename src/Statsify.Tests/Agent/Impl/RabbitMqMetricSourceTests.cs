using System;
using System.Net;
using NUnit.Framework;
using Statsify.Agent.Configuration;
using Statsify.Agent.Impl;

namespace Statsify.Tests.Agent.Impl
{
    [TestFixture]
    public class RabbitMqMetricSourceTests
    {
        [Test]
        public void GetMetricDefinitions()
        {
            var path = Environment.GetEnvironmentVariable("STATSIFY_RABBITMQ_PATH");
            if (string.IsNullOrWhiteSpace(path))
            {
                System.Diagnostics.Debug.WriteLine("Missing %STATSIFY_RABBITMQ_PATH% environment variable, is Rabbit installed?");
                return;
            }

            var configuration = new MetricConfiguration("rabbit_mq", "rabbit-mq", path, AggregationStrategy.Counter, null);
            var metricSource = new RabbitMqMetricSource(configuration);

            CollectionAssert.IsNotEmpty(metricSource.GetMetricDefinitions());
        }

        [Test]
        public void RewriteUrl()
        {
            var uri = new Uri("http://login:p%40ssword@rabbitmq.local:15672/");

            NetworkCredential credentials;
            RabbitMqMetricSource.RewriteUrl(ref uri, out credentials);

            Assert.AreEqual("login", credentials.UserName);
            Assert.AreEqual("p@ssword", credentials.Password);
            Assert.AreEqual(new Uri("http://rabbitmq.local:15672/api/overview"), uri);
        }
    }
}
