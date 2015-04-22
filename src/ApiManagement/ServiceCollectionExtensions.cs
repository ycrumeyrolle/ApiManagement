using System;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.Caching.Distributed;
using ApiManagement;
using Microsoft.Framework.Caching.Memory;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// The <see cref="IServiceCollection"/> extensions for enabling API management support.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add services needed to support throttling to the given <paramref name="serviceCollection"/>.
        /// </summary>
        /// <param name="serviceCollection">The service collection to which Throttling services are added.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddProxyCache(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions();
            serviceCollection.AddTransient<ISystemClock, SystemClock>();
            serviceCollection.AddSingleton<IDistributedCache, LocalCache>();
            serviceCollection.AddSingleton<IMemoryCache, MemoryCache>();

            return serviceCollection;
        }
    }
}