using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.Caching.Memory;
using System.IO;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Caching.Distributed;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Net.Http.Headers;
using System.Linq;
using Microsoft.AspNet.WebUtilities;

namespace ApiManagement
{
    public class ProxyCacheMiddleware
    {
        private const string CacheKeyTokenSeparator = "||";

        private static readonly char[] AttributeSeparator = new[] { ',' };

        private readonly RequestDelegate _next;
        private readonly ProxyCacheOptions _options;
        private readonly ISystemClock _clock;
        private readonly IDistributedCache _cache;
        private readonly CacheItemSerializer _serializer = new CacheItemSerializer();

        public ProxyCacheMiddleware(RequestDelegate next, IDistributedCache cache, ISystemClock clock, ProxyCacheOptions optionsAccessor)
        {
            _next = next;
            _cache = cache;
            _clock = clock;
            _options = optionsAccessor;
        }

        public async Task Invoke([NotNull] HttpContext context)
        {
            CacheContext cacheContext = new CacheContext(context);

            var requestHeaders = context.Request.GetTypedHeaders();
            var requestCacheControl = requestHeaders.CacheControl;

            if (cacheContext.CacheableRequest && (requestCacheControl != null && !requestCacheControl.NoCache))
            {
                var key = GenerateKey(context);
                Stream stream;
                if (_cache.TryGetValue(key, out stream))
                {
                    var cacheItem = _serializer.Deserialize(stream);

                    context.Response.StatusCode = cacheItem.StatusCode;
                    foreach (var kvpHeader in cacheItem.Headers)
                    {
                        context.Response.Headers.Set(kvpHeader.Key, kvpHeader.Value);
                    }

                    context.Response.Headers.Set("X-Cache", "HIT");
                    context.Response.Body.Write(cacheItem.Body, 0, cacheItem.Body.Length);
                    return;
                }
            }

            context.Response.OnSendingHeaders(state =>
            {
                context.Response.Headers.Set("X-Cache", "MISS");
            }, null);

            if (!context.Response.Body.CanRead)
            {
                context.Response.Body = new BufferingWriteStream(context.Response.Body);
            }

            await _next(context);

            if (!cacheContext.CacheableRequest)
            {
                return;
            }


            if (CanStore(cacheContext))
            {
                // TODO : use a BlockingCollection ?
                Store(cacheContext);
            }

            // TODO : Heuristics?
        }

        private bool CanStore(CacheContext cacheContext)
        {
            if (cacheContext.ResponseCacheControl == null)
            {
                return true;
            }

            if (cacheContext.ResponseCacheControl.NoStore || cacheContext.RequestCacheControl.NoStore)
            {
                return false;
            }

            if (cacheContext.ResponseCacheControl.Private)
            {
                return false;
            }

            // TODO : Use constant from Microsoft.Net.Http.Headers/HeaderNames.cs
            if (cacheContext.HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                if (!cacheContext.ResponseCacheControl.MustRevalidate && !cacheContext.ResponseCacheControl.Public && !cacheContext.ResponseCacheControl.SharedMaxAge.HasValue)
                {
                    return false;
                }
            }

            if (!cacheContext.ResponseCacheControl.MaxAge.HasValue && !cacheContext.ResponseCacheControl.SharedMaxAge.HasValue && !cacheContext.ResponseHeaders.Expires.HasValue)
            {
                return false;
            }

            if (!cacheContext.HttpContext.Response.Body.CanRead && !(cacheContext.HttpContext.Response.Body is BufferingWriteStream))
            {
                return false;
            }

            return true;
        }

        private void Store(CacheContext cacheContext)
        {
            var cacheKey = GenerateKey(cacheContext.HttpContext);
            var expires = ComputeExpiration(cacheContext.HttpContext);
            //Stream value;
            //if (_cache.Set(cacheKey.ToString(), out value))
            //{

            //}
            Stream stream = _serializer.Serialize(cacheContext.HttpContext.Response, expires);
            _cache.Set(cacheKey, null, cacheSetContext =>
            {
                stream.Position = 0L;
                stream.CopyTo(cacheSetContext.Data);
                // TODO : add expiration
            });
        }

        private DateTimeOffset ComputeExpiration([NotNull] HttpContext context)
        {
            return DateTimeOffset.UtcNow;
            // TODO : if shared cache : get 
            //var apparentAge = Math.Max(0, (long)(_clock.UtcNow - _clock.UtcNow).TotalMilliseconds);
            //var correctedReceivedAge = Math.Max(apparentAge, age_value);
            //response_delay = response_time - request_time;
            //corrected_initial_age = corrected_received_age + response_delay;
            //resident_time = now - response_time;
            //current_age = corrected_initial_age + resident_time;
        }
        //private DateTimeOffset ComputeExpiration([NotNull] HttpContext context)
        //{
        //    // TODO : if shared cache : get 
        //    var apparentAge = Math.Max(0, (long)(_clock.UtcNow - _clock.UtcNow).TotalMilliseconds);
        //    var correctedReceivedAge = Math.Max(apparentAge, age_value);
        //    response_delay = response_time - request_time;
        //    corrected_initial_age = corrected_received_age + response_delay;
        //    resident_time = now - response_time;
        //    current_age = corrected_initial_age + resident_time;
        //}

        private static string GenerateKey([NotNull] HttpContext context)
        {
            var request = context.Request;
            var builder = new StringBuilder(request.Method);
            builder
                .Append(CacheKeyTokenSeparator)
                .Append(request.Path.Value);
            //if (!string.IsNullOrEmpty(VaryBy))
            //{
            //    builder.Append(CacheKeyTokenSeparator)
            //           .Append(nameof(VaryBy))
            //           .Append(CacheKeyTokenSeparator)
            //           .Append(VaryBy);
            //}

            //AddStringCollectionKey(builder, nameof(VaryByCookie), VaryByCookie, request.Cookies);
            //AddStringCollectionKey(builder, nameof(VaryByHeader), VaryByHeader, request.Headers);
            //AddStringCollectionKey(builder, nameof(VaryByQuery), VaryByQuery, request.Query);
            //AddVaryByRouteKey(builder);

            //if (VaryByUser)
            //{
            //    builder.Append(CacheKeyTokenSeparator)
            //           .Append(nameof(VaryByUser))
            //           .Append(CacheKeyTokenSeparator)
            //           .Append(ViewContext.HttpContext.User?.Identity?.Name);
            //}

            // The key is typically too long to be useful, so we use a cryptographic hash
            // as the actual key (better randomization and key distribution, so small vary
            // values will generate dramatically different keys).
            using (var sha = SHA256.Create())
            {
                var contentBytes = Encoding.UTF8.GetBytes(builder.ToString());
                var hashedBytes = sha.ComputeHash(contentBytes);
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private static void AddStringCollectionKey(StringBuilder builder,
                                                     string keyName,
                                                     string value,
                                                     IReadableStringCollection sourceCollection)
        {
            if (!string.IsNullOrEmpty(value))
            {
                // keyName(param1=value1|param2=value2)
                builder.Append(CacheKeyTokenSeparator)
                       .Append(keyName)
                       .Append("(");

                var tokenFound = false;
                foreach (var item in Tokenize(value))
                {
                    tokenFound = true;

                    builder.Append(item)
                           .Append(CacheKeyTokenSeparator)
                           .Append(sourceCollection[item])
                           .Append(CacheKeyTokenSeparator);
                }

                if (tokenFound)
                {
                    // Remove the trailing separator
                    builder.Length -= CacheKeyTokenSeparator.Length;
                }

                builder.Append(")");
            }
        }

        private static IEnumerable<string> Tokenize(string value)
        {
            return value.Split(AttributeSeparator, StringSplitOptions.RemoveEmptyEntries)
                        .Select(token => token.Trim())
                        .Where(token => token.Length > 0);
        }
    }
}