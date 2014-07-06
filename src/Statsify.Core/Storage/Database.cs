using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Statsify.Core.Storage
{
    public class Database
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly byte[] Signature = Encoding.ASCII.GetBytes("STFY");
        private static readonly byte[] Version = BitConverter.GetBytes((ushort)(1 << 8));
        
        private const int ArchiveHeaderSize = sizeof(int) * 3;
        private const int DatapointSize = sizeof(long) + sizeof(double);

        private readonly string path;
        private readonly float downsamplingFactor;
        private readonly DownsamplingMethod downsamplingMethod;
        private readonly int maxRetention;
        private readonly IList<Archive> archives;
        private readonly Func<DateTime> currentTimeProvider;

        private Database(string path, float downsamplingFactor, DownsamplingMethod downsamplingMethod, int maxRetention, IList<Archive> archives, Func<DateTime> currentTimeProvider)
        {
            this.path = path;
            this.downsamplingFactor = downsamplingFactor;
            this.downsamplingMethod = downsamplingMethod;
            this.maxRetention = maxRetention;
            this.archives = archives;
            this.currentTimeProvider = currentTimeProvider;
        }

        public static bool Exists(string path)
        {
            return File.Exists(path);
        }

        public static Database OpenOrCreate(string path, float downsamplingFactor, DownsamplingMethod downsamplingMethod, RetentionPolicy retentionPolicy, Func<DateTime> currentTimeProvider = null)
        {
            EnsureValidRetentionPolicy(retentionPolicy);

            using(var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 8192, FileOptions.WriteThrough))
            {
                return fileStream.Length == 0 ? 
                    Create(path, fileStream, downsamplingFactor, downsamplingMethod, retentionPolicy, currentTimeProvider) : 
                    Open(path, fileStream, currentTimeProvider);
            } // using
        }

        public static Database Open(string path, Func<DateTime> currentTimeProvider = null)
        {   
            using(var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return Open(path, fileStream);
        }

        public static Database Create(string path, float downsamplingFactor, DownsamplingMethod downsamplingMethod, RetentionPolicy retentionPolicy, Func<DateTime> currentTimeProvider = null)
        {
            EnsureValidRetentionPolicy(retentionPolicy);

            using(var fileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 8192, FileOptions.WriteThrough))
                return Create(path, fileStream, downsamplingFactor, downsamplingMethod, retentionPolicy, currentTimeProvider);
        }

        private static Database Open(string path, Stream stream, Func<DateTime> currentTimeProvider = null)
        {
            using(var binaryReader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                var signature = binaryReader.ReadBytes(Signature.Length);
                if(!signature.SequenceEqual(Signature)) 
                    throw new DataException("Incompatible Statsify Database format");

                var version = new Version(binaryReader.ReadByte(), binaryReader.ReadByte());
                if(version != new Version(1, 0))
                    throw new DatabaseException("Incompatible Statsify Database version");

                var dowsamplingMethod = (DownsamplingMethod)binaryReader.ReadInt32();
                var downsamplingFactor = binaryReader.ReadSingle();
                var maxRetention = binaryReader.ReadInt32();
                var archivesLength = binaryReader.ReadInt32();
                
                var archives = new List<Archive>();
                
                for(var i = 0; i < archivesLength; ++i)
                {
                    var archiveOffsetPointer = binaryReader.ReadInt64();
                    var secondsPerPoint = binaryReader.ReadInt32();
                    var points = binaryReader.ReadInt32();

                    archives.Add(new Archive(archiveOffsetPointer, secondsPerPoint, points, secondsPerPoint * points, points * DatapointSize));
                } // for

                return new Database(path, downsamplingFactor, dowsamplingMethod, maxRetention, archives, currentTimeProvider);
            } // using
        }


        private static Database Create(string path, FileStream stream, float downsamplingFactor, DownsamplingMethod downsamplingMethod, RetentionPolicy retentionPolicy, Func<DateTime> currentTimeProvider = null)
        {
            var maxRetention = retentionPolicy.Select(h => h.SecondsPerPoint * h.Points).Max();

            var headerSize =
                sizeof(byte) * Signature.Length +
                sizeof(byte) * Version.Length +
                sizeof(int) +
                sizeof(float) +
                sizeof(int) +
                sizeof(int) +
                ArchiveHeaderSize * retentionPolicy.Count;

            var archives = new List<Archive>();

            using(var binaryWriter = new BinaryWriter(stream))
            {
                binaryWriter.Write(Signature);
                binaryWriter.Write(Version);

                binaryWriter.Write((int)downsamplingMethod);
                binaryWriter.Write((float)downsamplingFactor);
                binaryWriter.Write((int)maxRetention);
                binaryWriter.Write((int)retentionPolicy.Count);

                var archiveOffsetPointer = headerSize;

                foreach(var retention in retentionPolicy)
                {
                    binaryWriter.Write(archiveOffsetPointer);
                    binaryWriter.Write(retention.SecondsPerPoint);
                    binaryWriter.Write(retention.Points);

                    archives.Add(new Archive(archiveOffsetPointer, retention.SecondsPerPoint, retention.Points, retention.SecondsPerPoint * retention.Points,
                        retention.Points * DatapointSize));

                    archiveOffsetPointer += retention.Points * DatapointSize;
                } // foreach

                stream.SetLength(archiveOffsetPointer);
            } // using

            return new Database(path, downsamplingFactor, downsamplingMethod, maxRetention, archives, currentTimeProvider);
        }

        public Series ReadSeries(DateTime from, DateTime until, TimeSpan? precision = null)
        {
            using(var fileStream = File.OpenRead(path))
            using(var binaryReader = new BinaryReader(fileStream, Encoding.UTF8, true))
            {
                var fromTime = ConvertToTimestamp(from);
                var untilTime = ConvertToTimestamp(until);
                var now = ConvertToTimestamp(currentTimeProvider());

                if(fromTime > untilTime) throw new Exception(); // TODO: Exception class

                var oldestTime = now - maxRetention;

                if(fromTime > now) return null;
                if(untilTime < oldestTime) return null;

                if(fromTime < oldestTime)
                    fromTime = oldestTime;

                if(untilTime > now)
                    untilTime = now;

                var diff = now - fromTime;

                var archive = archives.First(a => a.Retention >= diff && (precision == null || a.SecondsPerPoint >= precision.Value.TotalSeconds));

                var fromInterval = (fromTime - (fromTime % archive.SecondsPerPoint)) + archive.SecondsPerPoint;
                var untilInterval = (untilTime - (untilTime % archive.SecondsPerPoint)) + archive.SecondsPerPoint;

                fileStream.Seek(archive.Offset, SeekOrigin.Begin);

                var baseInterval = binaryReader.ReadInt64();
                var baseValue = binaryReader.ReadDouble();

                var step = archive.SecondsPerPoint;

                if(baseInterval == 0)
                {
                    var pts = (untilInterval - fromInterval) / step;
                    var valueList = new double?[pts];

                    return new Series(ConvertFromTimestamp(fromInterval), ConvertFromTimestamp(untilInterval), TimeSpan.FromSeconds(step), valueList);
                } // if


                var fromOffset = GetOffset(fromInterval, baseInterval, archive);
                var untilOffset = GetOffset(untilInterval, baseInterval, archive);

                fileStream.Seek(fromOffset, SeekOrigin.Begin);

                byte[] seriesString = null;
                if(fromOffset < untilOffset)
                {
                    seriesString = new byte[untilOffset - fromOffset];
                    binaryReader.Read(seriesString, 0, seriesString.Length);
                } // if
                else
                {
                    var archiveEnd = archive.Offset + archive.Size;
                
                    var n1 = (int)(archiveEnd - fromOffset);
                    var n2 = (int)(untilOffset - archive.Offset);
                
                    seriesString = new byte[n1 + n2];
                    binaryReader.Read(seriesString, 0, n1);
                    fileStream.Seek(archive.Offset, SeekOrigin.Begin);
                    binaryReader.Read(seriesString, n1, n2);
                } // else

                var points = seriesString.Length / DatapointSize;
                var values = new double?[points];
                var currentInterval = fromInterval;

                using(var memoryStream = new MemoryStream(seriesString))
                using(var br = new BinaryReader(memoryStream))
                {
                    for(var i = 0; i < points; ++i)
                    {
                        var unpackedInterval = br.ReadInt64();
                        var unpackedValue = br.ReadDouble();

                        if(unpackedInterval == currentInterval)
                            values[i] = unpackedValue;

                        currentInterval += step;
                    } // for
                } // using

                return new Series(ConvertFromTimestamp(fromInterval), ConvertFromTimestamp(untilInterval), TimeSpan.FromSeconds(step), values);
            } // using
        }

        private static long GetOffset(long fromInterval, long baseInterval, Archive archive)
        {
            var timeDistance = fromInterval - baseInterval;
            var pointDistance = timeDistance / archive.SecondsPerPoint;
            var byteDistance = (pointDistance * DatapointSize) % archive.Size;
            var fromOffset = archive.Offset +
                             (byteDistance > 0
                                 ? byteDistance
                                 : archive.Size + byteDistance);
            return fromOffset;
        }

        public void WriteDatapoint(DateTime dateTime, double value)
        {
            using(var fileStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using(var binaryWriter = new BinaryWriter(fileStream, Encoding.UTF8, true))
            using(var binaryReader = new BinaryReader(fileStream, Encoding.UTF8, true))
            {
                var timestamp = ConvertToTimestamp(dateTime.ToUniversalTime());
                var now = ConvertToTimestamp(currentTimeProvider());

                var diff = now - timestamp;

                if(diff < 0) throw new Exception(); // TODO
                if(diff >= maxRetention) throw new Exception(); // TODO

                Archive archive = null;
                IList<Archive> lowerArchives = null;

                foreach(var i in archives.Select((a, i) => Tuple.Create(i, a)))
                {
                    if(i.Item2.Retention < diff) continue;
                    archive = i.Item2;
                    lowerArchives = new List<Archive>(archives.Skip(i.Item1 + 1));
                    break;
                } // foreach

                var myInterval = timestamp - (timestamp % archive.SecondsPerPoint);

                fileStream.Seek(archive.Offset, SeekOrigin.Begin);
                var baseInterval = binaryReader.ReadInt64();
                var baseValue = binaryReader.ReadDouble();

                if(baseInterval == 0)
                {
                    fileStream.Seek(archive.Offset, SeekOrigin.Begin);
                    binaryWriter.Write((long)myInterval);
                    binaryWriter.Write((double)value);

                    baseInterval = myInterval;
                    baseValue = value;
                }
                else
                {
                    var timeDistance = myInterval - baseInterval;
                    var pointDistance = timeDistance / archive.SecondsPerPoint;
                    var byteDistance = pointDistance * DatapointSize;
                    var myOffset = archive.Offset + (byteDistance % archive.Size);

                    fileStream.Seek(myOffset, SeekOrigin.Begin);
                    
                    binaryWriter.Write((long)myInterval);
                    binaryWriter.Write((double)value);
                } // else

                var higher = archive;
                foreach(var lower in lowerArchives)
                {
                    if(!Propagate(fileStream, binaryReader, binaryWriter, myInterval, higher, lower))
                        break;

                    higher = lower;
                } // foreach

                fileStream.Flush(true);
            } // using
        }

        private bool Propagate(FileStream fileStream, BinaryReader binaryReader, BinaryWriter binaryWriter, long timestamp, Archive higher, Archive lower)
        {
            var lowerIntervalStart = (timestamp - (timestamp % lower.SecondsPerPoint));
            var lowerIntervalEnd = (lowerIntervalStart + lower.SecondsPerPoint);

            fileStream.Seek(higher.Offset, SeekOrigin.Begin);
            var higherBaseInterval = binaryReader.ReadInt64();
            var higherBaseValue = binaryReader.ReadDouble();

            var higherFirstOffset = 0L;

            if(higherBaseInterval == 0)
            {
                higherFirstOffset = higher.Offset;
            } // if
            else
            {
                var timeDistance = (lowerIntervalStart - higherBaseInterval);
                var pointDistance = timeDistance / higher.SecondsPerPoint;
                var byteDistance = pointDistance * DatapointSize;
                higherFirstOffset = higher.Offset + (byteDistance % higher.Size);
            } // else

            var higherPoints = lower.SecondsPerPoint / higher.SecondsPerPoint;
            var higherSize = higherPoints * DatapointSize;
            var relativeFirstOffset = higherFirstOffset - higher.Offset;
            var relativeLastOffset = (relativeFirstOffset + higherSize) % higher.Size;
            var higherLastOffset = relativeLastOffset + higher.Offset;

            fileStream.Seek(higherFirstOffset, SeekOrigin.Begin);

            byte[] seriesString = null;
            if(higherFirstOffset < higherLastOffset)
            {
                seriesString = new byte[higherLastOffset - higherFirstOffset];
                binaryReader.Read(seriesString, 0, seriesString.Length);
            } // if
            else
            {
                var higherEnd = higher.Offset + higher.Size;
                
                var n1 = (int)(higherEnd - higherFirstOffset);
                var n2 = (int)(higherLastOffset - higher.Offset);
                
                seriesString = new byte[n1 + n2];
                binaryReader.Read(seriesString, 0, n1);
                fileStream.Seek(higher.Offset, SeekOrigin.Begin);
                binaryReader.Read(seriesString, n1, n2);
            } // else

            var points = seriesString.Length / DatapointSize;

            var neighborValues = new double[points];
            var currentInterval = lowerIntervalStart;
            var step = higher.SecondsPerPoint;
            var knownValues = 0;

            using(var memoryStream = new MemoryStream(seriesString))
            using(var br = new BinaryReader(memoryStream))
            {
                for(var i = 0; i < points; ++i)
                {
                    var unpackedInterval = br.ReadInt64();
                    var unpackedValue = br.ReadDouble();

                    if(unpackedInterval == currentInterval)
                        neighborValues[knownValues++] = unpackedValue;

                    currentInterval += step;
                } // for
            } // using

            if((float)knownValues / points < downsamplingFactor) return false;

            var aggregateValue = Aggregate(neighborValues, knownValues);

            fileStream.Seek(lower.Offset, SeekOrigin.Begin);

            var lowerBaseInterval = binaryReader.ReadInt64();
            var lowerBaseValue = binaryReader.ReadDouble();

            if(lowerBaseInterval == 0)
            {
                fileStream.Seek(lower.Offset, SeekOrigin.Begin);
            } // if
            else
            {
                var timeDistance = lowerIntervalStart - lowerBaseInterval;
                var pointDistance = timeDistance / lower.SecondsPerPoint;
                var byteDistance = pointDistance * DatapointSize;
                var lowerOffset = lower.Offset + (byteDistance % lower.Size);

                fileStream.Seek(lowerOffset, SeekOrigin.Begin);
            } // else

            binaryWriter.Write((long)lowerIntervalStart);
            binaryWriter.Write((double)aggregateValue);

            return true;
        }

        private double Aggregate(IEnumerable<double> neighborValues, int knownValues)
        {
            var values = neighborValues.Take(knownValues);

            switch(downsamplingMethod)
            {
                case DownsamplingMethod.Average:
                    return values.Average();
                case DownsamplingMethod.Sum:
                    return values.Sum();
                case DownsamplingMethod.Last:
                    return values.Last();
                case DownsamplingMethod.Max:
                    return values.Max();
                case DownsamplingMethod.Min:
                    return values.Min();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static long ConvertToTimestamp(DateTime value)
        {
            var elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        private static DateTime ConvertFromTimestamp(long timestamp)
        {
            return Epoch.AddSeconds(timestamp);
        }

        private static void EnsureValidRetentionPolicy(RetentionPolicy retentionPolicy)
        {
            RetentionPolicyValidator.EnsureRetentionPolicyValid(retentionPolicy);        
        }

    }
}
