using System;
using NUnit.Framework;
using Statsify.Web.Api.Configuration;
using Statsify.Web.Api.Services;

namespace Statsify.Tests.Web.Api.Services
{
    [TestFixture]
    public class SeriesServiceTests
    {
        [Test]
        public void GetSeries()
        {
            var seriesService = new SeriesService(new MetricService(@"\\mow1aps3\c$\statsify\data"));
            seriesService.GetSeries("Ema(AliasByNode(production.aeroclub_time.request.count, 2), 0.01)", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
        }
    }
}
