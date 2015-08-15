using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Statsify.Core.Model;

namespace Statsify.Core.Storage
{
    public class DatapointDatabase
    {
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
            Timestamp fromTimestamp = from;
            Timestamp untilTimestamp = until;
            Timestamp nowTimestamp = currentTimeProvider();

            if(fromTimestamp > untilTimestamp) throw new Exception(); // TODO: Exception class

            Timestamp oldestTime = nowTimestamp - maxRetention;

            if(fromTimestamp > nowTimestamp) return null;
            if(untilTimestamp < oldestTime) return null;

            if(fromTimestamp < oldestTime)
                fromTimestamp = oldestTime;

            if(untilTimestamp > nowTimestamp)
                untilTimestamp = nowTimestamp;

            Timestamp diff = nowTimestamp - fromTimestamp;

            var archive = archives.First(a => ((TimeSpan)a.Retention.History).TotalSeconds >= diff && (precision == null || a.Retention.Precision >= precision.Value));

            Timestamp fromInterval = (fromTimestamp - (fromTimestamp % archive.Retention.Precision)) + archive.Retention.Precision;
            Timestamp untilInterval = (untilTimestamp - (untilTimestamp % archive.Retention.Precision)) + archive.Retention.Precision;
            var step = archive.Retention.Precision;

            double?[] values = null;

            using(var fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using(var binaryReader = new Util.BinaryReader(fileStream, Encoding.UTF8, true))
            {
                var baseInterval = binaryReader.ReadInt64(archive.Offset);
                if(baseInterval == 0)
                {
                    var points = (untilInterval - fromInterval) / step;
                    values = new double?[points];
                } // if
                else
                {
                    var fromOffset = GetReadOffset(archive, fromInterval, baseInterval);
                    var untilOffset = GetReadOffset(archive, untilInterval, baseInterval);

                    var buffer = ReadBuffer(binaryReader, fromOffset, untilOffset, archive);

                    int knownValues;
                    UnpackDatapoints(archive, buffer, fromInterval, out values, out knownValues);
                } // else
            } // using
            
            var timestamps =
                Enumerable.Range(0, values.Length).
                    Select(i => new Timestamp(fromInterval + step * i));

            return new Series(fromInterval, untilInterval, TimeSpan.FromSeconds(step), 
                timestamps.Zip(values, (ts, v) => new Datapoint(ts, v)));
        }

        private static byte[] ReadBuffer(BinaryReader binaryReader, long fromOffset, long untilOffset, Archive archive)
        {
            binaryReader.BaseStream.Seek(fromOffset, SeekOrigin.Begin);

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
                binaryReader.BaseStream.Seek(archive.Offset, SeekOrigin.Begin);
                binaryReader.Read(buffer, n1, n2);
            } // else

            return buffer;
        }

        public void WriteDatapoints(IList<Datapoint> datapoints)
        {
            Timestamp now = currentTimeProvider();

            var archiveIndex = 0;
            var archive = archives[archiveIndex];

            var currentPoints = new List<Datapoint>();

            using(var fileStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using(var binaryWriter = new Util.BinaryWriter(fileStream, Encoding.UTF8, true))
            using(var binaryReader = new Util.BinaryReader(fileStream, Encoding.UTF8, true))
            {
                foreach(var point in datapoints.OrderByDescending(d => d.Timestamp))
                {
                    var age = TimeSpan.FromSeconds(now - (Timestamp)point.Timestamp);

                    while(archive.Retention.History < age)
                    {
                        if(currentPoints.Count > 0)
                        {
                            WriteDatapoints(binaryReader, binaryWriter, archive, Enumerable.Reverse(currentPoints).ToList());
                            currentPoints.Clear();
                        } // if

                        if(archiveIndex == archives.Count - 1)
                            break;

                        archive = archives[++archiveIndex];
                    } // while

                    if(archive == null)
                        break;

                    currentPoints.Add(point);
                } // foreach

                if(archive != null && currentPoints.Count > 0)
                    WriteDatapoints(binaryReader, binaryWriter, archive, Enumerable.Reverse(currentPoints).ToList());

                fileStream.Flush(true);
            } // using
        }

        public void WriteDatapoint(Datapoint datapoint)
        {
            if(!datapoint.Value.HasValue) return;

            WriteDatapoint(datapoint.Timestamp, datapoint.Value.Value);
        }

        public void WriteDatapoint(DateTime dateTime, double value)
        {
            Timestamp timestamp = dateTime.ToUniversalTime();
            Timestamp now = currentTimeProvider();

            var diff = now - timestamp;

            if(diff < 0) throw new Exception(); // TODO
            if(diff >= maxRetention) throw new Exception(); // TODO

            var archive = archives.FirstOrDefault(a => a.Retention.History >= diff);
            if(archive == null)
                throw new Exception(); // TODO: Exception

            var lowerArchives = archives.Skip(archives.IndexOf(archive) + 1);

            Timestamp myInterval = timestamp - (timestamp % archive.Retention.Precision);

            using(var fileStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using(var binaryWriter = new Util.BinaryWriter(fileStream, Encoding.UTF8, true))
            using(var binaryReader = new Util.BinaryReader(fileStream, Encoding.UTF8, true))
            {
                WriteDatapoint(binaryReader, binaryWriter, archive, myInterval, value);
                
                var higher = archive;
                foreach(var lower in lowerArchives)
                {
                    if(!Downsample(binaryReader, binaryWriter, myInterval, higher, lower))
                        break;

                    higher = lower;
                } // foreach

                fileStream.Flush(true);
            } // using
        }

        private static long GetReadOffset(Archive archive, Timestamp lowerIntervalStart, Timestamp higerBaseInterval)
        {
            if(higerBaseInterval == 0)
                return archive.Offset;

            var timeDistance = lowerIntervalStart - higerBaseInterval;
            var pointDistance = timeDistance / archive.Retention.Precision;
            var byteDistance = (pointDistance * DatapointSize) % archive.Size;
            
            var increment = 
                byteDistance > 0 ? 
                    byteDistance : 
                    archive.Size + byteDistance;
            
            var offset = archive.Offset + increment;
            
            return offset;
        }

        private static long GetWriteOffset(Archive archive, Timestamp baseInterval, Timestamp myInterval)
        {
            if(baseInterval == 0)
                return archive.Offset;
            
            Timestamp timeDistance = myInterval - baseInterval;
            var pointDistance = timeDistance / archive.Retention.Precision;
            var byteDistance = pointDistance * DatapointSize;

            var offset = archive.Offset + (byteDistance % archive.Size);

            return offset;
        }

        private bool Downsample(BinaryReader binaryReader, BinaryWriter binaryWriter, Timestamp timestamp, Archive higher, Archive lower)
        {
            Timestamp higherBaseInterval = binaryReader.ReadInt64(higher.Offset);

            Timestamp lowerIntervalStart = (timestamp - (timestamp % lower.Retention.Precision));
            var higherFirstOffset = GetReadOffset(higher, lowerIntervalStart, higherBaseInterval);

            var higherPoints = lower.Retention.Precision / higher.Retention.Precision;
            var higherSize = higherPoints * DatapointSize;
            var relativeFirstOffset = higherFirstOffset - higher.Offset;
            var relativeLastOffset = (relativeFirstOffset + higherSize) % higher.Size;
            var higherLastOffset = relativeLastOffset + higher.Offset;

            var buffer = ReadBuffer(binaryReader, higherFirstOffset, higherLastOffset, higher);

            double?[] values;
            int knownValues;

            var points = UnpackDatapoints(higher, buffer, lowerIntervalStart, out values, out knownValues);

            if((float)knownValues / points < downsamplingFactor) return false;

            var aggregateValue = Aggregate(values.Where(v => v.HasValue).Select(v => v.Value), downsamplingMethod);

            WriteDatapoint(binaryReader, binaryWriter, lower, lowerIntervalStart, aggregateValue);

            return true;
        }

        private void WriteDatapoints(BinaryReader binaryReader, BinaryWriter binaryWriter, Archive archive, IEnumerable<Datapoint> datapoints)
        {
            var step = (int)archive.Retention.Precision;
            var alignedPoints = datapoints.Select(d => {
                Timestamp ts = d.Timestamp;
                return Tuple.Create(new Timestamp(ts - (ts % step)), d.Value);
            }).ToList();

            Timestamp? previousInterval = null;
            var packedStrings = new List<Tuple<Timestamp, Tuple<Timestamp, double?>[]>>();
            var currentString = new List<Tuple<Timestamp, double?>>();

            for(var i = 0; i < alignedPoints.Count; ++i)
            {
                if(i < alignedPoints.Count - 1 && alignedPoints[i].Item1 == alignedPoints[i + 1].Item1)
                    continue;

                var interval = alignedPoints[i].Item1;
                var value = alignedPoints[i].Item2;

                if(!previousInterval.HasValue || (interval == previousInterval + step))
                {
                    currentString.Add(Tuple.Create(interval, value));
                    previousInterval = interval;
                } // if
                else
                {
                    var numberOfPoints = currentString.Count;
                    Timestamp startInterval = previousInterval.Value - (step * (numberOfPoints - 1));
                    packedStrings.Add(Tuple.Create(startInterval, currentString.ToArray()));

                    currentString.Clear();
                    currentString.Add(Tuple.Create(interval, value));

                    previousInterval = interval;
                } // else
            } // for

            if(currentString.Any() && previousInterval.HasValue)
            {
                var numberOfPoints = currentString.Count;
                Timestamp startInterval = previousInterval.Value - (step * (numberOfPoints - 1));
                packedStrings.Add(Tuple.Create(startInterval, currentString.ToArray()));
            } // if

            // TODO

            Timestamp baseInterval = binaryReader.ReadInt64(archive.Offset);
            if(baseInterval == 0)
                baseInterval = packedStrings[0].Item1;

            foreach(var t in packedStrings)
            {
                var interval = t.Item1;
                var packedString = t.Item2;

                var myOffset = GetWriteOffset(archive, interval, baseInterval);
                binaryWriter.BaseStream.Seek(myOffset, SeekOrigin.Begin);

                var archiveEnd = archive.Offset + archive.Size;
                var bytesBeyond = (int)(myOffset + packedString.Length * DatapointSize - archiveEnd);
                
                byte[] buffer;

                using(var ms = new MemoryStream())
                using(var bw = new BinaryWriter(ms))
                {
                    foreach(var dp in packedString)
                        WriteDatapoint(bw, dp.Item1, dp.Item2 ?? 0); // TODO questionable "?? 0"

                    bw.Flush();

                    buffer = ms.ToArray();
                }

                if(bytesBeyond > 0)
                {
                    var x = buffer.Length - bytesBeyond;
                    binaryWriter.Write(buffer, 0, x);

                    Debug.Assert(binaryWriter.BaseStream.Position == archiveEnd);
                    
                    binaryWriter.BaseStream.Seek(archive.Offset, SeekOrigin.Begin);
                    binaryWriter.Write(buffer, x, buffer.Length - x);
                }
                else
                    binaryWriter.Write(buffer);
            } // foreach

            var higher = archive;
            var lowerArchives = archives.Where(a => a.Retention.Precision > archive.Retention.Precision);

            Func<long, Archive, long> fit = (i, a) => i - (i % a.Retention.Precision);

            foreach(var lower in lowerArchives)
            {
                var uniqueLowerIntervals = new HashSet<Timestamp>(alignedPoints.Select(p => new Timestamp(fit(p.Item1, lower))));
                var propagateFurther = false;

                foreach(var interval in uniqueLowerIntervals.OrderBy(n => n))
                    propagateFurther |= Downsample(binaryReader, binaryWriter, interval, higher, lower);

                if(!propagateFurther)
                    break;

                higher = lower;
            } // foreach
        }

        private static void WriteDatapoint(BinaryReader binaryReader, BinaryWriter binaryWriter, Archive archive, Timestamp timestamp, double value)
        {
            var lowerBaseInterval = binaryReader.ReadInt64(archive.Offset);

            var offset = GetWriteOffset(archive, lowerBaseInterval, timestamp);
            WriteDatapoint(binaryWriter, offset, timestamp, value);
        }

        private static int UnpackDatapoints(Archive archive, byte[] buffer, Timestamp startInterval, out double?[] values, out int knownValues)
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

        private static void WriteDatapoint(BinaryWriter binaryWriter, long offset, long timestamp, double value)
        {
            binaryWriter.BaseStream.Seek(offset, SeekOrigin.Begin);

            WriteDatapoint(binaryWriter, timestamp, value);
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
