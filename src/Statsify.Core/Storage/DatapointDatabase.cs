using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Statsify.Core.Model;

namespace Statsify.Core.Storage
{
    public class DatapointDatabase
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly byte[] Signature = Encoding.ASCII.GetBytes("STFY");
        private static readonly byte[] Version = { 1, 0 };
        
        private const int ArchiveHeaderSize = sizeof(int) * 3;
        private const int DatapointSize = sizeof(long) + sizeof(double);

        private readonly string path;
        private readonly float downsamplingFactor;
        private readonly DownsamplingMethod downsamplingMethod;
        private readonly int maxRetention;
        private readonly IList<Archive> archives;
        private readonly Func<DateTime> currentTimeProvider;

        public ReadOnlyCollection<Archive> Archives
        {
            get { return new ReadOnlyCollection<Archive>(archives); }
        }

        public float DownsamplingFactor
        {
            get { return downsamplingFactor; }
        }

        public DownsamplingMethod DownsamplingMethod
        {
            get { return downsamplingMethod; }
        }

        private DatapointDatabase(string path, float downsamplingFactor, DownsamplingMethod downsamplingMethod, int maxRetention, IList<Archive> archives, Func<DateTime> currentTimeProvider)
        {
            this.path = path;
            this.downsamplingFactor = downsamplingFactor;
            this.downsamplingMethod = downsamplingMethod;
            this.maxRetention = maxRetention;
            this.archives = archives;
            this.currentTimeProvider = currentTimeProvider ?? (() => DateTime.UtcNow);
        }

        public static bool Exists(string path)
        {
            return File.Exists(path);
        }

        public static DatapointDatabase OpenOrCreate(string path, float downsamplingFactor, DownsamplingMethod downsamplingMethod, RetentionPolicy retentionPolicy, Func<DateTime> currentTimeProvider = null)
        {
            EnsureValidRetentionPolicy(retentionPolicy);

            using(var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 8192, FileOptions.WriteThrough))
            {
                return fileStream.Length == 0 ? 
                    Create(path, fileStream, downsamplingFactor, downsamplingMethod, retentionPolicy, currentTimeProvider) : 
                    Open(path, fileStream, currentTimeProvider);
            } // using
        }

        public static DatapointDatabase Open(string path, Func<DateTime> currentTimeProvider = null)
        {   
            using(var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return Open(path, fileStream, currentTimeProvider);
        }

        public static DatapointDatabase Create(string path, float downsamplingFactor, DownsamplingMethod downsamplingMethod, RetentionPolicy retentionPolicy, Func<DateTime> currentTimeProvider = null)
        {
            EnsureValidRetentionPolicy(retentionPolicy);

            using(var fileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 8192, FileOptions.WriteThrough))
                return Create(path, fileStream, downsamplingFactor, downsamplingMethod, retentionPolicy, currentTimeProvider);
        }

        private static DatapointDatabase Open(string path, Stream stream, Func<DateTime> currentTimeProvider = null)
        {
            using(var binaryReader = new Util.BinaryReader(stream, Encoding.UTF8, true))
            {
                var signature = binaryReader.ReadBytes(Signature.Length);
                if(!signature.SequenceEqual(Signature)) 
                    throw new DatabaseException("Incompatible Statsify Database format");

                var major = binaryReader.ReadByte();
                var minor = binaryReader.ReadByte();
                var version = new Version(major, minor);
                if(version != new Version(1, 0))
                    throw new DatabaseException("Incompatible Statsify Database version");

                var dowsamplingMethod = (DownsamplingMethod)binaryReader.ReadInt32();
                var downsamplingFactor = binaryReader.ReadSingle();
                var maxRetention = binaryReader.ReadInt32();
                var archivesLength = binaryReader.ReadInt32();
                
                var archives = new List<Archive>();
                
                for(var i = 0; i < archivesLength; ++i)
                {
                    var offset = binaryReader.ReadInt32();
                    var precision = binaryReader.ReadInt32();
                    var history = binaryReader.ReadInt32();

                    archives.Add(new Archive(offset, history * DatapointSize, new Retention(TimeSpan.FromSeconds(precision), history)));
                } // for

                return new DatapointDatabase(path, downsamplingFactor, dowsamplingMethod, maxRetention, archives, currentTimeProvider);
            } // using
        }


        private static DatapointDatabase Create(string path, FileStream stream, float downsamplingFactor, DownsamplingMethod downsamplingMethod, RetentionPolicy retentionPolicy, Func<DateTime> currentTimeProvider = null)
        {
            var maxRetention = retentionPolicy.Select(h => h.Precision * h.History).Max();

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
            
                // ReSharper disable RedundantCast
                binaryWriter.Write((int)downsamplingMethod);
                binaryWriter.Write((float)downsamplingFactor);
                binaryWriter.Write((int)maxRetention);
                binaryWriter.Write((int)retentionPolicy.Count);
                // ReSharper restore RedundantCast

                var offset = headerSize;

                foreach(var retention in retentionPolicy)
                {
                    // ReSharper disable RedundantCast
                    binaryWriter.Write((int)offset);
                    binaryWriter.Write((int)retention.Precision);
                    binaryWriter.Write((int)retention.History);
                    // ReSharper restore RedundantCast

                    archives.Add(new Archive(offset, retention.History * DatapointSize, retention));

                    offset += retention.History * DatapointSize;
                } // foreach

                stream.SetLength(offset);
            } // using

            return new DatapointDatabase(path, downsamplingFactor, downsamplingMethod, maxRetention, archives, currentTimeProvider);
        }

        public Series ReadSeries(DateTime from, DateTime until, TimeSpan? precision = null)
        {
            var fromTimestamp = ConvertToTimestamp(from);
            var untilTimestamp = ConvertToTimestamp(until);
            var nowTimestamp = ConvertToTimestamp(currentTimeProvider());

            if(fromTimestamp > untilTimestamp) throw new Exception(); // TODO: Exception class

            var oldestTime = nowTimestamp - maxRetention;

            if(fromTimestamp > nowTimestamp) return null;
            if(untilTimestamp < oldestTime) return null;

            if(fromTimestamp < oldestTime)
                fromTimestamp = oldestTime;

            if(untilTimestamp > nowTimestamp)
                untilTimestamp = nowTimestamp;

            var diff = nowTimestamp - fromTimestamp;

            var archive = archives.First(a => ((TimeSpan)a.Retention.History).TotalSeconds >= diff && (precision == null || a.Retention.Precision >= precision.Value));

            var fromInterval = (fromTimestamp - (fromTimestamp % archive.Retention.Precision)) + archive.Retention.Precision;
            var untilInterval = (untilTimestamp - (untilTimestamp % archive.Retention.Precision)) + archive.Retention.Precision;
            var step = archive.Retention.Precision;

            double?[] values = null;

            using(var fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using(var binaryReader = new Util.BinaryReader(fileStream, Encoding.UTF8, true))
            {
                fileStream.Seek(archive.Offset, SeekOrigin.Begin);

                var baseInterval = binaryReader.ReadInt64();
                if(baseInterval == 0)
                {
                    var points = (untilInterval - fromInterval) / step;
                    values = new double?[points];
                } // if
                else
                {
                    var fromOffset = GetOffset(fromInterval, baseInterval, archive);
                    var untilOffset = GetOffset(untilInterval, baseInterval, archive);

                    var buffer = ReadBuffer(fileStream, fromOffset, untilOffset, binaryReader, archive);

                    int knownValues;
                    UnpackDatapoints(archive, buffer, fromInterval, out values, out knownValues);
                } // else
            } // using
            
            var timestamps =
                Enumerable.Range(0, values.Length).
                    Select(i => ConvertFromTimestamp(fromInterval + step * i));

            return new Series(ConvertFromTimestamp(fromInterval), ConvertFromTimestamp(untilInterval), TimeSpan.FromSeconds(step), 
                timestamps.Zip(values, (ts, v) => new Datapoint(ts, v)));
        }

        private static byte[] ReadBuffer(FileStream fileStream, long fromOffset, long untilOffset, BinaryReader binaryReader, Archive archive)
        {
            fileStream.Seek(fromOffset, SeekOrigin.Begin);

            byte[] buffer;
            if(fromOffset < untilOffset)
            {
                buffer = new byte[untilOffset - fromOffset];
                binaryReader.Read(buffer, 0, buffer.Length);
            } // if
            else
            {
                var archiveEnd = archive.Offset + archive.Size;

                var n1 = (int)(archiveEnd - fromOffset);
                var n2 = (int)(untilOffset - archive.Offset);

                buffer = new byte[n1 + n2];
                binaryReader.Read(buffer, 0, n1);
                fileStream.Seek(archive.Offset, SeekOrigin.Begin);
                binaryReader.Read(buffer, n1, n2);
            } // else

            return buffer;
        }

        private static long GetOffset(long fromInterval, long baseInterval, Archive archive)
        {
            var timeDistance = fromInterval - baseInterval;
            var pointDistance = timeDistance / archive.Retention.Precision;
            var byteDistance = (pointDistance * DatapointSize) % archive.Size;
            
            var increment = 
                byteDistance > 0 ? 
                    byteDistance : 
                    archive.Size + byteDistance;
            
            var fromOffset = archive.Offset + increment;
            
            return fromOffset;
        }

        public void WriteDatapoint(Datapoint datapoint)
        {
            if(!datapoint.Value.HasValue) return;

            WriteDatapoint(datapoint.Timestamp, datapoint.Value.Value);
        }

        public void WriteDatapoint(DateTime dateTime, double value)
        {
            var timestamp = ConvertToTimestamp(dateTime.ToUniversalTime());
            var now = ConvertToTimestamp(currentTimeProvider());

            var diff = now - timestamp;

            if(diff < 0) throw new Exception(); // TODO
            if(diff >= maxRetention) throw new Exception(); // TODO

            var archive = archives.FirstOrDefault(a => a.Retention.History >= diff);
            if(archive == null)
                throw new Exception(); // TODO: Exception

            var lowerArchives = archives.Skip(archives.IndexOf(archive) + 1);

            var myInterval = timestamp - (timestamp % archive.Retention.Precision);

            using(var fileStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using(var binaryWriter = new Util.BinaryWriter(fileStream, Encoding.UTF8, true))
            using(var binaryReader = new Util.BinaryReader(fileStream, Encoding.UTF8, true))
            {
                fileStream.Seek(archive.Offset, SeekOrigin.Begin);
                var baseInterval = binaryReader.ReadInt64();

                if(baseInterval == 0)
                {
                    fileStream.Seek(archive.Offset, SeekOrigin.Begin);
                }
                else
                {
                    var timeDistance = myInterval - baseInterval;
                    var pointDistance = timeDistance / archive.Retention.Precision;
                    var byteDistance = pointDistance * DatapointSize;
                    var myOffset = archive.Offset + (byteDistance % archive.Size);

                    fileStream.Seek(myOffset, SeekOrigin.Begin);
                } // else

                WriteDatapoint(binaryWriter, myInterval, value);

                var higher = archive;
                foreach(var lower in lowerArchives)
                {
                    if(!Downsample(fileStream, binaryReader, binaryWriter, myInterval, higher, lower))
                        break;

                    higher = lower;
                } // foreach

                fileStream.Flush(true);
            } // using
        }

        private bool Downsample(FileStream fileStream, BinaryReader binaryReader, BinaryWriter binaryWriter, long timestamp, Archive higher, Archive lower)
        {
            var lowerIntervalStart = (timestamp - (timestamp % lower.Retention.Precision));

            fileStream.Seek(higher.Offset, SeekOrigin.Begin);
            var higherBaseInterval = binaryReader.ReadInt64();

            var higherFirstOffset = GetOffset(lowerIntervalStart, higherBaseInterval, higher);

            var higherPoints = lower.Retention.Precision / higher.Retention.Precision;
            var higherSize = higherPoints * DatapointSize;
            var relativeFirstOffset = higherFirstOffset - higher.Offset;
            var relativeLastOffset = (relativeFirstOffset + higherSize) % higher.Size;
            var higherLastOffset = relativeLastOffset + higher.Offset;

            var buffer = ReadBuffer(fileStream, higherFirstOffset, higherLastOffset, binaryReader, higher);

            double?[] values;
            int knownValues;

            var points = UnpackDatapoints(higher, buffer, lowerIntervalStart, out values, out knownValues);

            if((float)knownValues / points < downsamplingFactor) return false;

            var aggregateValue = Aggregate(values.Where(v => v.HasValue).Select(v => v.Value), downsamplingMethod);

            fileStream.Seek(lower.Offset, SeekOrigin.Begin);

            var lowerBaseInterval = binaryReader.ReadInt64();

            if(lowerBaseInterval == 0)
            {
                fileStream.Seek(lower.Offset, SeekOrigin.Begin);
            } // if
            else
            {
                var timeDistance = lowerIntervalStart - lowerBaseInterval;
                var pointDistance = timeDistance / lower.Retention.Precision;
                var byteDistance = pointDistance * DatapointSize;
                var lowerOffset = lower.Offset + (byteDistance % lower.Size);

                fileStream.Seek(lowerOffset, SeekOrigin.Begin);
            } // else

            WriteDatapoint(binaryWriter, lowerIntervalStart, aggregateValue);

            return true;
        }

        private static int UnpackDatapoints(Archive archive, byte[] buffer, long startInterval, out double?[] values, out int knownValues)
        {
            var points = buffer.Length / DatapointSize;
            values = new double?[points];

            var currentTimestamp = startInterval;
            var step = archive.Retention.Precision;
            
            knownValues = 0;

            using(var memoryStream = new MemoryStream(buffer))
            using(var binaryReader = new BinaryReader(memoryStream))
            {
                for(var i = 0; i < points; ++i)
                {
                    var timestamp = binaryReader.ReadInt64();
                    var value = binaryReader.ReadDouble();

                    if(timestamp == currentTimestamp)
                    {
                        values[i] = value;
                        knownValues++;
                    } // if

                    currentTimestamp += step;
                } // for
            } // using

            return points;
        }

        private static double Aggregate(IEnumerable<double> values, DownsamplingMethod downsamplingMethod)
        {
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
                    throw new ArgumentOutOfRangeException("downsamplingMethod");
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

        private static void WriteDatapoint(BinaryWriter binaryWriter, long timestamp, double value)
        {
            // ReSharper disable RedundantCast
            binaryWriter.Write((long)timestamp);
            binaryWriter.Write((double)value);
            // ReSharper restore RedundantCast
        }

        private static void EnsureValidRetentionPolicy(RetentionPolicy retentionPolicy)
        {
            RetentionPolicyValidator.EnsureRetentionPolicyValid(retentionPolicy);        
        }
    }
}
