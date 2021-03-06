﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
            using(var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 8192, FileOptions.WriteThrough))
            {
                return fileStream.Length == 0 ? 
                    Create(path, fileStream) : 
                    Open(path, fileStream);
            } // using
        }

        private static AnnotationDatabase Open(string path, FileStream stream)
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

        public void WriteAnnotation(DateTime timestamp, string title, string message, params string[] tags)
        {
            using(var fileStream = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.Read))
            using(var binaryWriter = new Util.BinaryWriter(fileStream, Encoding.UTF8, true))
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

                var tagCount = tags == null ? 0 : tags.Length;

                binaryWriter.Write(tagCount);

                if(tags != null)
                    foreach(var tag in tags)
                    {
                        buffer = Encoding.UTF8.GetBytes(tag);
                        binaryWriter.Write(buffer.Length);
                        binaryWriter.Write(buffer);
                    } // foreach

                binaryWriter.Flush();
            } // using
        }

        public IList<Annotation> ReadAnnotations(DateTime from, DateTime until)
        {
            using(var fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using(var binaryReader = new Util.BinaryReader(fileStream, Encoding.UTF8, true))
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

                var tags = new List<string>();
                var tagCount = 0;

                if(!binaryReader.TryReadInt32(out tagCount)) yield break;

                for(var i = 0; i < tagCount; ++i)
                {
                    if(!binaryReader.TryReadInt32(out bytes)) yield break;
                    if(!binaryReader.TryRead(bytes, out buffer)) yield break;

                    var tag = Encoding.UTF8.GetString(buffer);

                    tags.Add(tag);
                } // for

                var annotation = new Annotation(ConvertFromTimestamp(timestamp), title, message, tags);

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
}
