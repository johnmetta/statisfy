using System;
using Statsify.Core.Model;

namespace Statsify.Aggregator.Api.Services
{
    public interface ISeriesService
    {
        Series[] GetSeries(string query, DateTime start, DateTime stop);
    }
}
