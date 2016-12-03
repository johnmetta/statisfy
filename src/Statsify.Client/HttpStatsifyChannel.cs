using System;
using System.Net;

namespace Statsify.Client
{
    internal class HttpStatsifyChannel : IStatsifyChannel
    {
        private readonly Uri uri;

        public HttpStatsifyChannel(Uri uri)
        {
            this.uri = uri;
        }

        public void WriteBuffer(byte[] buffer)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Method = "POST";
            httpWebRequest.MediaType = "application/vnd.statsify.datagram+binary";

            using(var requestStream = httpWebRequest.GetRequestStream())
                requestStream.Write(buffer, 0, buffer.Length);

            using(httpWebRequest.GetResponse()) { } // using
        }

        public void Dispose()
        {
        }
    }
}