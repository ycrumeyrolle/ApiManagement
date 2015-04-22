using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using ApiManagement;

namespace ApiManagementFull
{
    public class Startup
    {
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddProxyCache();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UsePerformanceCounter();
            app.UseProxyCache();

            app.Use(next =>
            {
                return async context =>
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Hello!");
                };
            });
        }
    }
}
