using System;

namespace Statsify.Core.Storage
{
    public class Archive
    {
        public long Offset { get; private set; }

        public int Size { get; private set; }

        public Retention Retention { get; private set; }

        public Archive(long offset, int size, Retention retention)
        {
            Offset = offset;
            Size = size;
            Retention = retention;
        }
    }
}