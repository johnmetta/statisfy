using System;
using System.Globalization;
using Statsify.Core.Storage;

namespace Statsify.Web.Api
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

            DateTime result;
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result) ? 
                result : 
                @default;
        }
    }
}