using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using Statsify.Core.Storage;

namespace Statsify.Aggregator.Configuration
{
    [DebuggerDisplay("{Precision,nq} - {History,nq}")]
    public class RetentionConfigurationElement : ConfigurationElement, IRetentionDefinition
    {
        [ConfigurationProperty("precision", IsRequired = true)]
        public string Precision
        {
            get { return (string)this["precision"]; }
            set { this["precision"] = value; }
        }

        [ConfigurationProperty("history", IsRequired = true)]
        public string History
        {
            get { return (string)this["history"]; }
            set { this["history"] = value; }
        }

        TimeSpan IRetentionDefinition.Precision
        {
            get
            {
                var precision = ParseTimeSpan(Precision);
                if(precision == null) 
                    throw new ConfigurationException();

                return precision.Value;
            }
        }

        TimeSpan IRetentionDefinition.History
        {
            get
            {
                var history = ParseTimeSpan(History);
                if(history == null) 
                    throw new ConfigurationException();

                return history.Value;
            }
        }

        public static TimeSpan? ParseTimeSpan(string textValue)
        {
            TimeSpan timeSpan;
            if(TimeSpan.TryParse(textValue, out timeSpan))
                return timeSpan;

            var suffix = textValue.Last();
            
            int value;
            if(int.TryParse(textValue.Substring(0, textValue.Length - 1), out value))
                return ParseTimeSpan(suffix, value);

            return null;
        }

        private static TimeSpan? ParseTimeSpan(char suffix, int value)
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