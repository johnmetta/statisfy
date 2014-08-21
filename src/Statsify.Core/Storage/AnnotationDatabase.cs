using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Statsify.Core.Storage
{
    public class AnnotationDatabase
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly byte[] Signature = Encoding.ASCII.GetBytes("STFY");
        private static readonly byte[] Version = { 1, 0 };

        private const int HeaderSize = sizeof(byte) * (4 + 2);

        private readonly string path;

        private AnnotationDatabase(string path)
        {
            this.path = path;
        }

        public static AnnotationDatabase Create(string path)
        {
            using(var fileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 8192, FileOptions.WriteThrough))
                return Create(path, fileStream);
        }

        public static AnnotationDatabase OpenOrCreate(string path)
        {
            using(var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 8192, FileOptions.WriteThrough))
            {
                return fileStream.Length == 0 ? 
                    Create(path, fileStream) : 
                    Open(path, fileStream);
            } // using
        }

        private static AnnotationDatabase Open(string path, FileStream stream)
        {
            using(var binaryReader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                var signature = binaryReader.ReadBytes(Signature.Length);
                if(!signature.SequenceEqual(Signature))
                    throw new DatabaseException("Incompatible Statsify Database format");

                var major = binaryReader.ReadByte();
                var minor = binaryReader.ReadByte();
                var version = new Version(major, minor);
                if(version != new Version(1, 0))
                    throw new DatabaseException("Incompatible Statsify Database version");
            } // using

            return new AnnotationDatabase(path);
        }

        private static AnnotationDatabase Create(string path, FileStream stream)
        {
            using(var binaryWriter = new BinaryWriter(stream))
            {
                binaryWriter.Write(Signature);
                binaryWriter.Write(Version);

                binaryWriter.Flush();
            } // using

            return new AnnotationDatabase(path);
        }

        public void WriteAnnotation(DateTime timestamp, string title, string message)
        {
            using(var fileStream = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.Read))
            using(var binaryWriter = new BinaryWriter(fileStream, Encoding.UTF8, true))
            {
                fileStream.Seek(0, SeekOrigin.End);

                var ts = ConvertToTimestamp(timestamp.ToUniversalTime());
                binaryWriter.Write(ts);

                var buffer = Encoding.UTF8.GetBytes(title);
                binaryWriter.Write(buffer.Length);
                binaryWriter.Write(buffer);

                buffer = Encoding.UTF8.GetBytes(message);
                binaryWriter.Write(buffer.Length);
                binaryWriter.Write(buffer);

                binaryWriter.Flush();
            } // using
        }

        public IList<Annotation> ReadAnnotations(DateTime from, DateTime until)
        {
            using(var fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using(var binaryReader = new BinaryReader(fileStream, Encoding.UTF8, true))
            {
                fileStream.Seek(HeaderSize, SeekOrigin.Begin);

                var annotations =
                    ReadAnnotations(binaryReader).
                        Where(a => from <= a.Timestamp && a.Timestamp <= until).
                        ToList();
                
                return annotations;
            } // using
        }

        private IEnumerable<Annotation> ReadAnnotations(BinaryReader binaryReader)
        {
            var timestamp = 0L;
            var bytes = 0;
            byte[] buffer = null;

            while(binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
            {
                if(!binaryReader.TryReadInt64(out timestamp)) yield break;
                if(!binaryReader.TryReadInt32(out bytes)) yield break;
                if(!binaryReader.TryRead(bytes, out buffer)) yield break;

                var title = Encoding.UTF8.GetString(buffer);

                if(!binaryReader.TryReadInt32(out bytes)) yield break;
                if(!binaryReader.TryRead(bytes, out buffer)) yield break;

                var message = Encoding.UTF8.GetString(buffer);

                var annotation = new Annotation(ConvertFromTimestamp(timestamp), title, message);

                yield return annotation;
            } // while
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

    }

    internal static class BinaryReaderExtensions
    {
        public static bool TryRead(this BinaryReader binaryReader, int bytes, out byte[] value)
        {
            return TryRead(binaryReader, br => br.ReadBytes(bytes), bytes, out value);
        }

        public static bool TryReadInt32(this BinaryReader binaryReader, out int value)
        {
            return TryRead(binaryReader, br => br.ReadInt32(), sizeof(int), out value);
        }

        public static bool TryReadInt64(this BinaryReader binaryReader, out long value)
        {
            return TryRead(binaryReader, br => br.ReadInt64(), sizeof(long), out value);
        }

        private static bool TryRead<T>(this BinaryReader binaryReader, Func<BinaryReader, T> reader, int bytes, out T value)
        {
            if(!Buff(binaryReader, bytes))
            {
                value = default(T);
                return false;
            } // if

            value = reader(binaryReader);
            return true;
        }

        private static bool Buff(BinaryReader binaryReader, int bytes)
        {
            var stream = binaryReader.BaseStream;
            return stream.Position + bytes <= stream.Length;
        }
    }
}
