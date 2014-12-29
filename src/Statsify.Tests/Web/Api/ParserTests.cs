using System;
using System.Globalization;
using NUnit.Framework;
using Statsify.Web.Api;

namespace Statsify.Tests.Web.Api
{
    [TestFixture]
    public class ParserTests
    {
        [Test]
        public void ParseDateTime()
        {
            //Assert.AreEqual(ToUtc(new DateTime(2010, 12, 11)), Parser.ParseDateTime("2010-12-11", DateTime.MinValue, DateTime.MinValue).ToUniversalTime());
            //Assert.AreEqual(ToUtc(new DateTime(2010, 12, 11)), Parser.ParseDateTime("11.12.2010", DateTime.MinValue, DateTime.MinValue).ToUniversalTime());


            Console.WriteLine(Parser.ParseDateTime("2014-12-1 14:00", DateTime.MinValue, DateTime.MinValue).ToUniversalTime());
        }

        private static DateTime ToUtc(DateTime dateTime)
        {
            return dateTime;
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
    }
}
