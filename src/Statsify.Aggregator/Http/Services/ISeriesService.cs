using System;
using Statsify.Core.Model;

namespace Statsify.Aggregator.Http.Services
{
    public interface ISeriesService
    {
        Series[] GetSeries(string query, DateTime start, DateTime stop);
    }
}
