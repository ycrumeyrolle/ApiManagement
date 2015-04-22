using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.Caching.Memory;
using System.IO;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Caching.Distributed;
using System.Text;
using Microsoft.Net.Http.Headers;
using System.Linq;
using Microsoft.AspNet.WebUtilities;


namespace ApiManagement
{
    public class CacheItemSerializer
    {
        private static readonly string[] HopByHopHeaders =
        {
            "Connection",
            "Keep-Alive",
            "Proxy-Authenticate",
            "Proxy-Authorization",
            "TE",
            "Trailers",
            "Transfer-Encoding",
            "Upgrade"
        };

        public Stream Serialize([NotNull]HttpResponse response, DateTimeOffset expires)
        {
            var memory = new MemoryStream();

            using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
            {
                Write(writer, response, expires);
            }

            return memory;
        }

        public static void Write([NotNull] BinaryWriter writer, [NotNull] HttpResponse response, DateTimeOffset expires)
        {
            writer.Write(response.StatusCode);
            writer.Write(expires.Ticks);
            var headerKeys = response.Headers.Keys.Except(HopByHopHeaders).ToArray();
            writer.Write(headerKeys.Length);
            for (int i = 0; i < headerKeys.Length; i++)
            {
                var header = response.Headers.Get(headerKeys[i]);
                writer.Write(headerKeys[i]);
                writer.Write(header);
            }

            var stream = response.Body.CanRead ? response.Body : ((BufferingWriteStream)response.Body).Buffer;
            stream.Position = 0L;
            writer.Write(stream.Length);
            stream.CopyTo(writer.BaseStream);
        }

        public CacheItem Deserialize([NotNull]Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                return Read(reader);
            }
        }

        public static CacheItem Read([NotNull] BinaryReader reader)
        {
            CacheItem cacheItem = new CacheItem();
            cacheItem.StatusCode = reader.ReadInt32();
            cacheItem.Expires = new DateTimeOffset(reader.ReadInt64(), TimeSpan.Zero);
            var headerCount = reader.ReadInt32();
            for (int i = 0; i < headerCount; i++)
            {
                var headerKey = reader.ReadString();
                var header = reader.ReadString();
                cacheItem.Headers.Add(headerKey, header);
            }

            var bodyLength = reader.ReadInt64();
            if (bodyLength > Int32.MaxValue)
            {
                throw new InvalidOperationException("Unable to manage response of size greater than 2GB.");
            }

            cacheItem.Body = reader.ReadBytes((int)bodyLength);
            return cacheItem;
        }
    }
}