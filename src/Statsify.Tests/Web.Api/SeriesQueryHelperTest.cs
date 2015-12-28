using System;
using NUnit.Framework;
using Statsify.Web.Api.Helpers;

namespace Statsify.Tests.Web.Api
{
    [TestFixture]
    public class SeriesQueryHelperTest
    {
        [Test]
        public void ExtractSeriesListExpressionFromQuery()
        {            
            Assert.AreEqual("servers.n46-msk.system.processor.*",
                SeriesQueryHelper.ExtractSeriesListExpressionFromQuery("Abs(servers.n46-msk.system.processor.*)"));

            Assert.AreEqual("servers.n46-msk.system.processor.*",
                SeriesQueryHelper.ExtractSeriesListExpressionFromQuery("servers.n46-msk.system.processor.*"));

            Assert.AreEqual("servers.n46-msk.system.processor.*",
               SeriesQueryHelper.ExtractSeriesListExpressionFromQuery("Avg(Abs(Limit(servers.n46-msk.system.processor.*,5)))"));
        }

        [Test]
        public void ReplaceSeriesListExpression()
        {
            const string alias = "seriesList";
            
            Assert.AreEqual(String.Format("Abs({0})",alias),
               SeriesQueryHelper.ReplaceSeriesListExpression("Abs(servers.n46-msk.system.processor.*)", alias));

            Assert.AreEqual(alias,
                SeriesQueryHelper.ReplaceSeriesListExpression("servers.n46-msk.system.processor.*", alias));

            Assert.AreEqual(String.Format("Avg(Abs(Limit({0},5)))", alias),
               SeriesQueryHelper.ReplaceSeriesListExpression("Avg(Abs(Limit(servers.n46-msk.system.processor.*,5)))",alias));
        }
    }
}
