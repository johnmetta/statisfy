using System;
using System.Linq;
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
