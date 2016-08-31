using System;
using System.Globalization;

namespace Statsify.Core.Util
{
    public static class DateTimeParser
    {
        public static DateTime ParseDateTime(string text, DateTime now, DateTime @default)
        {
            text = (text ?? "").Trim();

            if(string.IsNullOrWhiteSpace(text))
                return @default;
            
            if(text.StartsWith("-"))
            {
                var offset = TimeSpanParser.ParseTimeSpan(text.Substring(1));
                return offset.HasValue ? 
                    now.Subtract(offset.Value) : 
                    @default;
            } // else

            //
            // This one is to support Grafana
            var timestamp = 0L;
            if(long.TryParse(text, out timestamp))
                return DateTimeUtil.FromUnixTimestamp(timestamp);

            DateTime result;
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result) ? 
                result : 
                @default;
        }
    }
}
