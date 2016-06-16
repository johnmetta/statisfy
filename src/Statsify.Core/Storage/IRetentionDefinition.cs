using System;

namespace Statsify.Core.Storage
{
    public interface IRetentionDefinition
    {
        TimeSpan Precision { get; }

        TimeSpan History { get; }
    }
}