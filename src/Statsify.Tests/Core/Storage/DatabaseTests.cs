using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statsify.Core.Storage;

namespace Statsify.Tests.Core.Storage
{
    [TestFixture]
    public class DatabaseTests
    {
        [Test]
        public void CreateOpen()
        {
            var path = Path.GetTempFileName();

            Database.Create(path, 0.5f, DownsamplingMethod.Sum, 
                new RetentionPolicy {
                    { TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1) },
                    { TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5) },
                    { TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(20) }
                });

            var database = Database.Open(path);

            Assert.AreEqual(0.5f, database.DownsamplingFactor);
            Assert.AreEqual(DownsamplingMethod.Sum, database.DownsamplingMethod);

            Assert.AreEqual(3, database.Archives.Count);

            Assert.AreEqual((TimeSpan)database.Archives[0].Retention.Precision, TimeSpan.FromSeconds(1));
            Assert.AreEqual((TimeSpan)database.Archives[0].Retention.History, TimeSpan.FromMinutes(1));

            Assert.AreEqual((TimeSpan)database.Archives[1].Retention.Precision, TimeSpan.FromSeconds(10));
            Assert.AreEqual((TimeSpan)database.Archives[1].Retention.History, TimeSpan.FromMinutes(5));

            Assert.AreEqual((TimeSpan)database.Archives[2].Retention.Precision, TimeSpan.FromSeconds(20));
            Assert.AreEqual((TimeSpan)database.Archives[2].Retention.History, TimeSpan.FromMinutes(20));
        }

        [Test]
        public void Create()
        {
            var n = 0;
            var date = new DateTime(1971, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            Func<DateTime> currentTimeProvider = () => date.AddSeconds(n);

            var database = 
                Database.Create(Path.GetTempFileName(), 0.5f, DownsamplingMethod.Last, 
                    new RetentionPolicy {
                        { TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1) },
                        { TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1) },
                        { TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(1) }
                    }, 
                    currentTimeProvider);

            for(n = 0; n < 120; ++n)
                database.WriteDatapoint(currentTimeProvider(), n);

            n -= 1;
            var now = currentTimeProvider();

            var series = database.ReadSeries(now.AddSeconds(-60), now);
            CollectionAssert.AreEqual(
                Enumerable.Range(60, 60).Select(v => (double?)v).ToArray(),
                series.Values);

            series = database.ReadSeries(now.AddSeconds(-60), now, TimeSpan.FromSeconds(10));
            CollectionAssert.AreEqual(
                new double?[] { 69, 79, 89, 99, 109, 119 }, 
                series.Values);

            series = database.ReadSeries(now.AddSeconds(-60), now, TimeSpan.FromSeconds(20));
            CollectionAssert.AreEqual(
                new double?[] { 79, 99, 119 },
                series.Values);
        }
    }
}
