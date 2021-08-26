using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Utils;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using System;
using System.Linq;
using System.Reflection;

namespace Omnikeeper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var version = VersionService.GetVersion();
            Console.WriteLine($"Running version: {version}");

            AddAssemblyResolver();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((ctx, builder) =>
                {
                    builder.AddConfiguration(ctx.Configuration.GetSection("Logging"));

                    builder.AddFile(ctx.Configuration.GetSection("Logging"));

                    builder.AddProvider(new HangfireConsoleLoggerProvider());
                    builder.Services.AddSingleton<ILoggerProvider>(sp => new ReactiveLoggerProvider(sp.GetRequiredService<ReactiveLogReceiver>()));
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

        /// <summary>
        /// NOTE: this method hooks into assembly resolving and provides hangfire with already loaded assemblies
        /// this is required so that hangfire jobs can be loaded via plugins.
        /// See https://stackoverflow.com/questions/47828704/how-to-use-hangfire-when-using-mef-to-load-plugins for further explanation
        /// </summary>
        private static void AddAssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
                var asmName = new AssemblyName(args.Name!);
                var existing = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(c => c.FullName == asmName.FullName);
                if (existing != null)
                {
                    return existing;
                }
                return null;
            };
        }
    }
}
