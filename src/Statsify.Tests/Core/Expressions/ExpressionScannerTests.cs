using System.Linq;
using NUnit.Framework;
using Statsify.Core.Expressions;

namespace Statsify.Tests.Core.Expressions
{
    [TestFixture]
    public class ExpressionScannerTests
    {
        [Test]
        public void ScanNumber()
        {
            var scanner = new ExpressionScanner();
            var token = scanner.Scan("123").First();

            Assert.AreEqual(TokenType.Integer, token.Type);
            Assert.AreEqual("123", token.Lexeme);

            token = scanner.Scan("-1293281").First();

            Assert.AreEqual(TokenType.Integer, token.Type);
            Assert.AreEqual("-1293281", token.Lexeme);

            token = scanner.Scan("129.3281").First();

            Assert.AreEqual(TokenType.Float, token.Type);
            Assert.AreEqual("129.3281", token.Lexeme);

            token = scanner.Scan("-293847.2724379324").First();

            Assert.AreEqual(TokenType.Float, token.Type);
            Assert.AreEqual("-293847.2724379324", token.Lexeme);

            token = scanner.Scan("-.2724379324").First();

            Assert.AreEqual(TokenType.Float, token.Type);
            Assert.AreEqual("-.2724379324", token.Lexeme);
        }

        [Test]
        public void Scan()
        {
            var scanner = new ExpressionScanner();
            var tokens = scanner.Scan("asPercent(Server01.connections.{failed,succeeded}, Server01.connections.attempted)").ToArray();
        }
    }
}
