using System;
using System.IO;
using NUnit.Framework;
using Statsify.Core.Storage;

namespace Statsify.Tests.Core.Storage
{
    [TestFixture]
    public class DatabaseTests
    {
        [Test]
        public void Create()
        {
            var n = 0;
            var date = new DateTime(1970, 1, 1, 0, 0, 1, DateTimeKind.Utc);

            Func<DateTime> currentTimeProvider = () => date.AddSeconds(n);

            var database = 
                Database.Create(Path.GetTempFileName(), 0.5f, DownsamplingMethod.Average, 
                    new RetentionPolicy {
                        { TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1) },
                        { TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1) },
                        { TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(1) }
                    }, 
                    currentTimeProvider);

            for(n = 0; n <= 60 * 30; ++n)
                database.WriteDatapoint(currentTimeProvider(), n);

            var now = currentTimeProvider();

            var datapoints = database.ReadSeries(now.AddSeconds(-60), now);
            Assert.AreEqual(60, datapoints.Values.Length);

            datapoints = database.ReadSeries(now.AddSeconds(-60), now, TimeSpan.FromSeconds(10));
            Assert.AreEqual(6, datapoints.Values.Length);

            datapoints = database.ReadSeries(now.AddSeconds(-60), now, TimeSpan.FromSeconds(20));
            Assert.AreEqual(3, datapoints.Values.Length);
        }
    }
}
