using System.Linq;
using NUnit.Framework;
using Statsify.Agent.Impl;

namespace Statsify.Tests.Agent.Impl
{
    [TestFixture]
    public class AveragerTests
    {
        [Test]
        public void GetOutliers()
        {
            var averager = new Averager();
            averager.Record("a", 1);
            averager.Record("a", 2);
            averager.Record("a", 3);
            averager.Record("a", 2);

            averager.Record("b", 5);
            averager.Record("b", 4);
            averager.Record("b", 5);
            averager.Record("b", 3);

            averager.Record("a", 8);

            var outlier = averager.GetOutliers(3).Single();

            Assert.AreEqual("a", outlier.Name);
            Assert.AreEqual(8d, outlier.LastValue);
        }
    }
}
