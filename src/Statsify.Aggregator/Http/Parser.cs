using System;
using System.Globalization;
using Statsify.Core.Storage;

namespace Statsify.Aggregator.Http
{
    public class Parser
    {
        public static DateTime ParseDateTime(string value, DateTime now, DateTime @default)
        {
            value = (value ?? "").Trim();

            if(string.IsNullOrWhiteSpace(value))
                return @default;
            
            if(value.StartsWith("-"))
            {
                var offset = RetentionPolicy.ParseTimeSpan(value.Substring(1));
                return offset.HasValue ? 
                    now.Subtract(offset.Value) : 
                    @default;
            } // else

            //
            // This one is to support Grafana
            var seconds = 0L;
            if(long.TryParse(value, out seconds))
                return DateTime.MinValue.AddSeconds(seconds);

            DateTime result;
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result) ? 
                result : 
                @default;
        }
    }
}