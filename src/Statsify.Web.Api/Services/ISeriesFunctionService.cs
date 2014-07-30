namespace Statsify.Web.Api.Services
{
    using Models;

    public interface ISeriesFunctionService
    {                   
        /// <summary>
        /// Takes one metric or a wildcard seriesList and applies the mathematical abs function to each datapoint transforming it to its absolute value.
        /// </summary>
        /// <param name="seriesList"></param>
        /// <returns></returns>
        Series[] Absolute(Series[] seriesList);

        /// <summary>
        /// Takes one metric or a wildcard seriesList and applies the mathematical abs function to each datapoint transforming it to its absolute value.
        /// </summary>
        /// <param name="seriesList"></param>
        /// <returns></returns>
        Series[] Abs(Series[] seriesList);

        /// <summary>
        /// Takes one metric or a wildcard seriesList. Draws the average value of all metrics passed at each time.
        /// </summary>
        /// <param name="seriesList"></param>
        /// <returns></returns>
        Series[] AverageSeries(Series[] seriesList);

        /// <summary>
        /// Takes one metric or a wildcard seriesList. Draws the average value of all metrics passed at each time.
        /// </summary>
        /// <param name="seriesList"></param>
        /// <returns></returns>
        Series[] Avg(Series[] seriesList);

        /// <summary>
        /// Takes one metric or a wildcard seriesList and a string in quotes. Prints the string instead of the metric name in the legend.
        /// </summary>
        /// <param name="seriesList"></param>
        /// <param name="alias"></param>
        /// <returns></returns>
        Series[] Alias(Series[] seriesList, string alias);

        /// <summary>
        /// Calculates a percentage of the total of a wildcard series. If total is specified, each series will be calculated as a percentage of that total. If total is not specified, the sum of all points in the wildcard series will be used instead.
        /// </summary>
        /// <param name="seriesList"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        Series[] AsPercent(Series[] seriesList, double total);

        /// <summary>
        /// Calculates a percentage of the total of a wildcard series. If total is specified, each series will be calculated as a percentage of that total. If total is not specified, the sum of all points in the wildcard series will be used instead.
        /// </summary>
        /// <param name="seriesList"></param>        
        /// <returns></returns>
        Series[] AsPercent(Series[] seriesList);

        /// <summary>
        /// Takes one metric or a wildcard seriesList followed by an integer N. 
        /// Only draw the first N metrics. Useful when testing a wildcard in a metric.
        /// </summary>
        /// <param name="seriesList"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        Series[] Limit(Series[] seriesList, int n);

        /// <summary>
        /// Takes a seriesList and applies an alias derived from the base metric name.
        /// </summary>
        /// <param name="seriesList"></param>
        /// <returns></returns>
        Series[] AliasByMetric(Series[] seriesList);

        /// <summary>
        /// Takes a seriesList and applies an alias derived from one or more “node” portion/s of the target name. Node indices are 0 indexed.
        /// <example>&expression=AliasByNode(List("servers.*.system.processor.*"),1,4)        
        /// </example>        
        /// </summary>
        /// <param name="seriesList"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        Series[] AliasByNode(Series[] seriesList, params int[] nodes);

        Series[] Sum(Series[] seriesList);

        Series[] SumSeries(Series[] seriesList);

        Series[] CountSeries(Series[] seriesList);

        Series[] Integral(Series[] seriesList);

        Series[] NonNegativeDerivative(Series[] seriesList);

        Series[] NonNegativeDerivative(Series[] seriesList, double? maxValue);

        Series[] MinimumAbove(Series[] seriesList, double n);

        Series[] MaximumBelow(Series[] seriesList, double n);

    }
}
