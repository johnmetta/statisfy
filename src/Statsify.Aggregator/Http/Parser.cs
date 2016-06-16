using System;
using System.Globalization;
using Statsify.Core.Storage;
using Statsify.Core.Util;

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
            var timestamp = 0L;
            if(long.TryParse(value, out timestamp))
                return DateTimeUtil.FromUnixTimestamp(timestamp);

            DateTime result;
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result) ? 
                result : 
                @default;
        }
    }
}