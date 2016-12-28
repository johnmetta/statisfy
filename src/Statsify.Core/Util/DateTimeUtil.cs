using System;

namespace Statsify.Core.Util
{
    public static class DateTimeUtil
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long? ToUnixTimestamp(this DateTime? value)
        {
            if (!value.HasValue) return null;

            var elapsedTime = value.Value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        public static long ToUnixTimestamp(this DateTime value)
        {
            return ToUnixTimestamp(new DateTime?(value)).GetValueOrDefault();
        }

        public static long? ToUnixTimestamp(this TimeSpan? value)
        {
            if (!value.HasValue)
                return null;

            return (long)value.Value.TotalSeconds;
        }

        public static long ToUnixTimestamp(this TimeSpan value)
        {
            return ToUnixTimestamp(new TimeSpan?(value)).GetValueOrDefault();
        }

        public static DateTime FromUnixTimestamp(long timestamp)
        {
            return Epoch.AddSeconds(timestamp);
        }
    }
}
