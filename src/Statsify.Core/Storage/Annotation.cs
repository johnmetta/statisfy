using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Statsify.Core.Storage
{
    [DebuggerDisplay("{Timestamp}: {Title,nq}")]
    public class Annotation
    {
        public DateTime Timestamp { get; private set; }

        public string Title { get; private set; }
 
        public string Message { get; private set; }

        public ReadOnlyCollection<string> Tags { get; private set; } 

        public Annotation(DateTime timestamp, string title, string message, IEnumerable<string> tags = null)
        {
            Timestamp = timestamp;
            Title = title;
            Message = message;
            Tags = new ReadOnlyCollection<string>(new List<string>(tags ?? new List<string>()));
        }
    }
}