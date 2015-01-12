using System.Linq;
using NUnit.Framework;
using Statsify.Core.Util;

namespace Statsify.Tests.Core.Util
{
    [TestFixture]
    public class EnumerableUtilTests
    {
        [Test]
        public void ToRanges()
        {
            var ranges = new[] { 1, 2, 3, 5 }.ToRanges().ToArray();

            CollectionAssert.AreEqual(
                new[] {
                    new Range<int>(1, 2),
                    new Range<int>(2, 3),
                    new Range<int>(3, 5)
                },
                ranges);
        }
    }
}
