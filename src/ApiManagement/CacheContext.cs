using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Headers;
using Microsoft.Net.Http.Headers;

namespace ApiManagement
{
    public class CacheContext
    {
        public CacheContext(HttpContext context)
        {
            HttpContext = context;

            var responseHeaders = context.Response.GetTypedHeaders();
            var responseCacheControl = responseHeaders.CacheControl;
        }

        public bool CacheableRequest
        {
            get
            {
                var method = HttpContext.Request.Method;
                return method == "GET" || method == "HEAD";
            }
        }

        public HttpContext HttpContext { get; private set; }

        public CacheControlHeaderValue RequestCacheControl { get { return RequestHeaders.CacheControl; } }

        public CacheControlHeaderValue ResponseCacheControl { get { return ResponseHeaders.CacheControl; } }

        public RequestHeaders RequestHeaders { get { return HttpContext.Request.GetTypedHeaders(); } }

        public ResponseHeaders ResponseHeaders { get { return HttpContext.Response.GetTypedHeaders(); } }
    }
}