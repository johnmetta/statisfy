using System;

namespace Statsify.Core.Expressions
{
    public class EvalContext
    {
        public DateTime From { get; private set; }

        public DateTime Until { get; private set; }

        public EvalContext(DateTime @from, DateTime until)
        {
            From = @from;
            Until = until;
        }
    }
}