using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Statsify.Core.Storage
{
    public class RetentionPolicy : IEnumerable<Retention>
    {
        private readonly IList<Retention> retentions = new List<Retention>();

        public int Count
        {
            get { return retentions.Count; }
        }
 
        public void Add(TimeSpan precision, TimeSpan history)
        {
            if(precision.TotalSeconds < 1) throw new ArgumentOutOfRangeException("precision");
            if(history.TotalSeconds < 1) throw new ArgumentOutOfRangeException("history");

            retentions.Add(new Retention(precision, history));
        }

        public IEnumerator<Retention> GetEnumerator()
        {
            return retentions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static RetentionPolicy Parse(string text)
        {
            var retentionPolicy = new RetentionPolicy();
            var parts = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim());

            foreach(var part in parts)
            {
                var subparts = part.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(subparts.Length != 2) continue;

                var precision = ParseTimeSpan(subparts[0]);
                var history = ParseTimeSpan(subparts[1]);

                if(!precision.HasValue || !history.HasValue) continue;

                retentionPolicy.Add(precision.Value, history.Value);
            } // foreach

            return retentionPolicy;
        }

        public static TimeSpan? ParseTimeSpan(string text)
        {
            var suffix = text.Last();
            var value = int.Parse(text.Substring(0, text.Length - 1));

            return ParseTimeSpan(suffix, value);
        }

        private static TimeSpan? ParseTimeSpan(char suffix, int value)
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
                    return TimeSpan.FromDays(value * 7);
                case 'y':
                    return TimeSpan.FromDays(365.25 * value);
                default:
                    throw new ArgumentOutOfRangeException("suffix");
            } // switch
        }
    }
}