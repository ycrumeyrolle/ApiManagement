using System;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace ApiManagement
{
    public static class PerformanceCounterExtensions
    {
        /// <summary>
        /// Enable directory browsing with the given options
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UsePerformanceCounter([NotNull] this IApplicationBuilder app)
        {
            return app.UseMiddleware<PerformanceCounterMiddleware>();
        }
    }
}
