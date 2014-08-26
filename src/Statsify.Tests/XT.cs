using System.Linq;
using NUnit.Framework;
using Statsify.Core.Expressions;

namespace Statsify.Tests
{
    [TestFixture]
    public class XT
    {
        [Test]
        public void T()
        {
            var x = new X(@"c:\statsify");
            var mn = x.GetMetricNames("servers.*.system.processor.total*").ToList();
        }
    }
}
