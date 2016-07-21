using System;
using System.Linq;

namespace Statsify.Core.Util
{
    public static class TimeSpanParser
    {
        public static TimeSpan? ParseTimeSpan(string text, DateTime? now = null)
        {
            TimeSpan timeSpan;
            return TryParseTimeSpan(text, out timeSpan, now) ? timeSpan : (TimeSpan?)null;
        }

        public static bool TryParseTimeSpan(string text, out TimeSpan timeSpan, DateTime? now = null)
        {
            timeSpan = TimeSpan.MinValue;
            if(string.IsNullOrWhiteSpace(text))
                return false;

            var suffix = text.Last();
            
            int value;
            if(int.TryParse(text.Substring(0, text.Length - 1), out value))
            {
                timeSpan = ParseTimeSpan(suffix, value, now);
                return true;
            } // if
            
            return false;
        }

        private static TimeSpan ParseTimeSpan(char suffix, int value, DateTime? now)
        {
            switch(Char.ToLower(suffix))
            {
                case 's':
                    return TimeSpan.FromSeconds(value);
                case 'm':
                    return TimeSpan.FromMinutes(value);
                case 'h':
                    return TimeSpan.FromHours(value);
                case 'd':
                    return TimeSpan.FromDays(value);
                case 'w':
                    return TimeSpan.FromDays(value * 7);
                case 'y':
                    return TimeSpan.FromDays(365.25 * value);
                default:
                    throw new ArgumentOutOfRangeException("suffix");
            } // switch
        }

    }
}
