using System;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace ApiManagement
{
    public static class ProxyCacheExtensions
    {
        /// <summary>
        /// Enable throttling on the current path
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseProxyCache([NotNull] this IApplicationBuilder app)
        {
            return app.UseProxyCache(new ProxyCacheOptions());
        }

        /// <summary>
        /// Enable directory browsing with the given options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseProxyCache([NotNull] this IApplicationBuilder app, ProxyCacheOptions options)
        {
            return app.UseMiddleware<ProxyCacheMiddleware>(options);
        }
    }
}
