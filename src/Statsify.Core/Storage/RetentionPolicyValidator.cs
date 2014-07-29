using System;
using System.Linq;

namespace Statsify.Core.Storage
{
    public class RetentionPolicyValidator
    {
        public static void EnsureRetentionPolicyValid(RetentionPolicy retentionPolicy)
        {
            if(retentionPolicy.Count == 0) 
                throw new RetentionPolicyValidationException("A Statsify database requires at least one Retention");

            var retentions = retentionPolicy.OrderBy(r => r.Precision).ToList();

            for(var i = 1; i < retentions.Count; ++i)
            {
                var previousRetention = retentions[i - 1];

                var retention = retentions[i];

                if(previousRetention.Precision == retention.Precision) 
                    throw new RetentionPolicyValidationException(
                        string.Format("A Statsify database may not be created having two Retentions (#{0}, #{1}) with the same precision", i - 1, i));
                
                if(retention.Precision % previousRetention.Precision != 0)
                    throw new RetentionPolicyValidationException(
                        string.Format("Higher precision Retention (#{0}, {1}) must evenly divide lower precision Retention (#{2}, {3})",
                            i - 1, previousRetention.Precision, i, retention.Precision));

                if((TimeSpan)retention.History < previousRetention.History)
                    throw new RetentionPolicyValidationException(
                        string.Format("Lower precision Retention (#{0}, {1}) must cover larger time intervals than higher precision Retention (#{2}, {3})",
                            i, retention.History, i - 1, previousRetention.History));

                var pointsPerDownsample = (long)retention.Precision / previousRetention.Precision;

                if(previousRetention.History < pointsPerDownsample)
                    throw new RetentionPolicyValidationException(
                        string.Format("Retention (#{0}) must have at least enough points to consolidate to the next Retention", i - 1));
            }
        }
    }
}