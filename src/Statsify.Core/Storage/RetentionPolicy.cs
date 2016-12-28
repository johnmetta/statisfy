using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Statsify.Core.Util;

namespace Statsify.Core.Storage
{
    public class RetentionPolicy : IEnumerable<Retention>
    {
        private readonly IList<Retention> retentions = new List<Retention>();

        public int Count
        {
            get { return retentions.Count; }
        }

        public RetentionPolicy()
        {
        }

        public RetentionPolicy(IEnumerable<IRetentionDefinition> retentions)
        {
            this.retentions = new List<Retention>(retentions.Select(r => new Retention(r.Precision, r.History)));    
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

                var precision = TimeSpanParser.ParseTimeSpan(subparts[0]);
                var history = TimeSpanParser.ParseTimeSpan(subparts[1]);

                if(!precision.HasValue || !history.HasValue) continue;

                retentionPolicy.Add(precision.Value, history.Value);
            } // foreach

            return retentionPolicy;
        }
    }
}