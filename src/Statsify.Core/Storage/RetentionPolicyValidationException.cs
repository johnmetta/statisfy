using System;

namespace Statsify.Core.Storage
{
    public class RetentionPolicyValidationException : Exception
    {
        public RetentionPolicyValidationException()
        {
        }

        public RetentionPolicyValidationException(string message) : 
            base(message)
        {
        }

        public RetentionPolicyValidationException(string message, Exception innerException) : 
            base(message, innerException)
        {
        }
    }
}