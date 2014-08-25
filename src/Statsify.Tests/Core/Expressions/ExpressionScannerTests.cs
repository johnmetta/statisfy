using System.Linq;
using NUnit.Framework;
using Statsify.Core.Expressions;

namespace Statsify.Tests.Core.Expressions
{
    [TestFixture]
    public class ExpressionScannerTests
    {
        [Test]
        public void Scan()
        {
            var scanner = new ExpressionScanner();
            var tokens = scanner.Scan("asPercent(Server01.connections.{failed,succeeded}, Server01.connections.attempted)").ToArray();
        }
    }
}
