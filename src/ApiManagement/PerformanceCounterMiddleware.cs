using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.Caching.Memory;
using System.Globalization;
using Microsoft.Framework.Internal;

namespace ApiManagement
{
    public class PerformanceCounterMiddleware
    {
        private readonly ISystemClock _clock;
        private readonly RequestDelegate _next;

        public PerformanceCounterMiddleware([NotNull] RequestDelegate next, [NotNull]  ISystemClock clock)
        {
            _next = next;
            _clock = clock;
        }

        public Task Invoke([NotNull] HttpContext context)
        {
            var start = _clock.UtcNow.Ticks;
            context.Response.OnSendingHeaders(state =>
            {
                var duration = TimeSpan.FromTicks(Math.Max(_clock.UtcNow.Ticks - start, 0L));

                context.Response.Headers.Set("X-Time-Taken", duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
            }, null);
            return _next(context);
        }
    }
}