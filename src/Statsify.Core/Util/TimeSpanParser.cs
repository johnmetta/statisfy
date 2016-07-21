﻿using System;
using System.Linq;

namespace Statsify.Core.Util
{
    public static class TimeSpanParser
    {
        public static bool TryParseTimeSpan(string text, out TimeSpan timeSpan)
        {
            timeSpan = TimeSpan.MinValue;
            if(string.IsNullOrWhiteSpace(text))
                return false;

            var suffix = text.Last();
            
            int value;
            if(int.TryParse(text.Substring(0, text.Length - 1), out value))
            {
                timeSpan = ParseTimeSpan(suffix, value);
                return true;
            } // if
            
            return false;
        }

        private static TimeSpan ParseTimeSpan(char suffix, int value)
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
