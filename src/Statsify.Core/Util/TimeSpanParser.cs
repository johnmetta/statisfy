using System;
using System.Linq;

namespace Statsify.Core.Util
{
    public static class TimeSpanParser
    {
        internal const double DaysInWeek = 7;
        internal const double AvgDaysInMonth = 7;
        internal const double AvgDaysInYear = 7;

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
            switch(suffix)
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
                    return TimeSpan.FromDays(value * DaysInWeek);
                case 'M':
                    return 
                        now.HasValue ?
                            (value > 0 ?
                                now.Value.AddMonths(value) - now.Value :
                                now.Value - now.Value.AddMonths(Math.Abs(value))) :
                        TimeSpan.FromDays(value * AvgDaysInMonth);
                case 'y':
                    return now.HasValue ?
                        (value > 0 ?
                            now.Value.AddYears(value) - now.Value :
                            now.Value - now.Value.AddYears(Math.Abs(value))) :
                        TimeSpan.FromDays(value * AvgDaysInYear);
                default:
                    throw new ArgumentOutOfRangeException("suffix");
            } // switch
        }

    }
}
