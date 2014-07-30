using System.Linq;
using Statsify.Core.Storage;
using Statsify.Web.Api.Helpers;

namespace Statsify.Web.Api.Services
{
    using System;
    using ExpressionEvaluator;
    using Series = Models.Series;

    internal class SeriesService : ISeriesService
    {        
        private readonly IMetricService metricService;

        public SeriesService(IMetricService metricService)
        {
            this.metricService = metricService;            
        }

        public Series[] GetSeries(string query, DateTime start, DateTime stop)
        {
            var seriesListExpression = SeriesQueryHelper.ExtractSeriesListExpressionFromQuery(query);

            var seriesList = List(seriesListExpression, start, stop);

            const string seriesListAlias = "seriesList";

            var evaluatorQuery = SeriesQueryHelper.ReplaceSeriesListExpression(query, seriesListAlias);

            var expr = new CompiledExpression(evaluatorQuery) { TypeRegistry = new TypeRegistry() };

            expr.TypeRegistry.Clear();

            expr.TypeRegistry.RegisterSymbol(seriesListAlias, seriesList);//Register seriesList parameter

            var seriesListFunctionService = new SeriesFunctionService(start, stop, seriesListExpression);

            seriesList = expr.ScopeCompile<ISeriesFunctionService>()(seriesListFunctionService) as Series[];            

            return seriesList;
        }
            
        private Series[] List(string seriesListExpression, DateTime start, DateTime stop)
        {
            var metrics = metricService.Find(seriesListExpression);

            return (from metric in metrics.Where(m => m.IsLeaf)
                    let databaseFilePath = metric.Info.FullName
                    let db = Database.Open(databaseFilePath)
                    let s = db.ReadSeries(start, stop)                   
                    select new Series(s.From,s.Until,s.Interval,s.Values)
                    {
                        Metric = metric,
                        Target = metric.Path
                    }).ToArray();
        }
    }
}
