using System;
using System.Diagnostics;

namespace Statsify.Core.Storage
{
    [DebuggerDisplay("{Timestamp}: {Title,nq}")]
    public class Annotation
    {
        public DateTime Timestamp { get; private set; }

        public string Title { get; private set; }
 
        public string Message { get; private set; }

        public Annotation(DateTime timestamp, string title, string message)
        {
            Timestamp = timestamp;
            Title = title;
            Message = message;
        }
    }
}