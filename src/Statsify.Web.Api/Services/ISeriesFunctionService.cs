namespace Statsify.Web.Api.Services
{
    using Models;

    public interface ISeriesFunctionService
    {
        Series[] List(string expression);
            
        /// <summary>
        /// Takes one metric or a wildcard seriesList and applies the mathematical abs function to each datapoint transforming it to its absolute value.
        /// </summary>
        /// <param name="series"></param>
        /// <returns></returns>
        Series[] Absolute(Series[] series);

        /// <summary>
        /// Takes one metric or a wildcard seriesList. Draws the average value of all metrics passed at each time.
        /// </summary>
        /// <param name="series"></param>
        /// <returns></returns>
        Series[] AverageSeries(Series[] series);

        /// <summary>
        /// Takes one metric or a wildcard seriesList and a string in quotes. Prints the string instead of the metric name in the legend.
        /// </summary>
        /// <param name="series"></param>
        /// <param name="alias"></param>
        /// <returns></returns>
        Series[] Alias(Series[] series, string alias);

        /// <summary>
        /// Calculates a percentage of the total of a wildcard series. If total is specified, each series will be calculated as a percentage of that total. If total is not specified, the sum of all points in the wildcard series will be used instead.
        /// </summary>
        /// <param name="series"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        Series[] AsPercent(Series[] series, double total);

        /// <summary>
        /// Calculates a percentage of the total of a wildcard series. If total is specified, each series will be calculated as a percentage of that total. If total is not specified, the sum of all points in the wildcard series will be used instead.
        /// </summary>
        /// <param name="series"></param>        
        /// <returns></returns>
        Series[] AsPercent(Series[] series);

        /// <summary>
        /// Takes one metric or a wildcard seriesList followed by an integer N. 
        /// Only draw the first N metrics. Useful when testing a wildcard in a metric.
        /// </summary>
        /// <param name="series"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        Series[] Limit(Series[] series, int n);

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
        /// <param name="series"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        Series[] AliasByNode(Series[] series, params int[] nodes);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="series"></param>
        /// <returns></returns>
        Series[] Sum(Series[] series);

    }
}
