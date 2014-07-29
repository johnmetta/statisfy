namespace Statsify.Web.Api.Services
{
    using System;
    using Models;

    public interface ISeriesService
    {
        Series[] GetSeries(string expression, DateTime start, DateTime stop);
    }
}
