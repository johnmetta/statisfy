namespace Statsify.Core.Storage
{
    public class Archive
    {
        public long Offset { get; private set; }

        public int SecondsPerPoint { get; private set; }

        public int Points { get; private set; }

        public int Retention { get; private set; }

        public int Size { get; private set; }

        public Archive(long offset, int secondsPerPoint, int points, int retention, int size)
        {
            Offset = offset;
            SecondsPerPoint = secondsPerPoint;
            Points = points;
            Retention = retention;
            Size = size;
        }
    }
}