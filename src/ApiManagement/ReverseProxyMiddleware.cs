using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.OptionsModel;
using System.IO;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNet.WebUtilities;

namespace ApiManagement
{
    public class ReverseProxyMiddleware
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly ReverseProxyOptions _options;

        public ReverseProxyMiddleware([NotNull] IOptions<ReverseProxyOptions> optionsAccessor)
        {
            _options = optionsAccessor.Options;
        }

        public async Task Invoke([NotNull] HttpContext context)
        {
            var requestMessage = CreateRequestMessage(context.Request);

            var responseMessage = await _client.SendAsync(requestMessage);

            await FillResponse(context.Response, responseMessage);
        }

        private async Task FillResponse(HttpResponse response, HttpResponseMessage responseMessage)
        {
            foreach (var header in responseMessage.Headers)
            {

            }

            response.StatusCode = (int)responseMessage.StatusCode;
            await responseMessage.Content.CopyToAsync(response.Body);
        }

        private HttpRequestMessage CreateRequestMessage(HttpRequest request)
        {
            var requestMessage = new HttpRequestMessage
            {
                Method = ConvertHttpMethod(request.Method),
                Content = request.Body == Stream.Null ? null : new StreamContent(request.Body),
                Version = ConvertProtocolToVersion(request.Protocol),
                RequestUri = new Uri(_options.TargetUrl + request.Path.Value)
            };

            foreach (var header in request.Headers)
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // TODO
            //foreach (var cookie in request.Cookies)
            //{
            //    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
            //}

            return requestMessage;
        }

        private static Version ConvertProtocolToVersion(string protocol)
        {
            return new Version(1, 1);
        }

        private static HttpMethod ConvertHttpMethod(string method)
        {
            switch (method)
            {
                case "DELETE":
                    return HttpMethod.Delete;
                case "GET":
                    return HttpMethod.Get;
                case "HEAD":
                    return HttpMethod.Head;
                case "OPTIONS":
                    return HttpMethod.Options;
                case "POST":
                    return HttpMethod.Post;
                case "PUT":
                    return HttpMethod.Put;
                case "TRACE":
                    return HttpMethod.Trace;
                default:
                    return HttpMethod.Get;
            }
        }
    }
}