using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statsify.Core.Util;

namespace Statsify.Tests.Core.Util
{
    [TestFixture]
    public class EnumerableExtensionsTests
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

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AllEqualOnEmptyCollection()
        {
            new int[]{ }.AllEqual(EqualityComparer<int>.Default);
        }

        [Test]
        public void AllEqual()
        {
            Assert.IsTrue(new[]{ 1 }.AllEqual(EqualityComparer<int>.Default));
            Assert.IsTrue(new[]{ 2, 2 }.AllEqual(EqualityComparer<int>.Default));
            Assert.IsFalse(new[]{ 3, 4 }.AllEqual(EqualityComparer<int>.Default));
        }
    }
}
