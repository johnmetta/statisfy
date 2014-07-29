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

        public Series[] GetSeries(string expression, DateTime start, DateTime stop)
        {
            var seriesListFunctionService = SeriesFunctionService.GetSeriesFunctionService(start, stop, metricService);      

            var expr = new CompiledExpression(expression);

            var series = expr.ScopeCompile<ISeriesFunctionService>()(seriesListFunctionService);

            return series as Series[];
        }
    }
}
