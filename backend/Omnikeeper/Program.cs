using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using System;

namespace Omnikeeper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var version = VersionService.GetVersion();
            Console.WriteLine($"Running version: {version}");

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((ctx, builder) =>
                {
                    builder.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                    builder.AddFile(o => o.RootPath = ctx.HostingEnvironment.ContentRootPath);
                    builder.AddProvider(new HangfireConsoleLoggerProvider());
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup.Startup>();
                })
                .UseDefaultServiceProvider((context, options) =>
                {
                    options.ValidateScopes = true;
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<Startup.HangfireJobStarter>();
                });
    }
}
