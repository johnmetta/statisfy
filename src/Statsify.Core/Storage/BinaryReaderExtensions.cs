using System;
using System.IO;

namespace Statsify.Core.Storage
{
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

        /// <summary>
        /// Reads <see cref="Int64"/> value first seeking to <paramref name="offset"/> on <paramref name="binaryReader"/>.
        /// </summary>
        /// <param name="binaryReader"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static long ReadInt64(this BinaryReader binaryReader, long offset)
        {
            binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);
            return binaryReader.ReadInt64();
        }
    }
}