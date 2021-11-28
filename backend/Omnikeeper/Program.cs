using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql.Logging;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Omnikeeper
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var version = VersionService.GetVersion();
            Console.WriteLine($"Running version: {version}");

            AddAssemblyResolver();

            var host = CreateHostBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .Build();

            using (var scope = host.Services.CreateScope())
            {
                NpgsqlLogManager.Provider = scope.ServiceProvider.GetRequiredService<NpgsqlLoggingProvider>();

                // migration/rebuild of *-latest tables in database to be backward compatible
                await RebuildLatestTablesIfNonEmpty(scope);

                // create a default __okconfig layer if it does not exist and meta config has this set
                await CreateOKConfigLayerIfNotExists(scope);
            }

            host.Run();
        }

        private static async Task CreateOKConfigLayerIfNotExists(IServiceScope scope)
        {
            var modelContextBuilder = scope.ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var metaConfigurationModel = scope.ServiceProvider.GetRequiredService<IMetaConfigurationModel>();
            var layerModel = scope.ServiceProvider.GetRequiredService<ILayerModel>();
            using (var mc = modelContextBuilder.BuildDeferred())
            {
                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(mc);
                if (metaConfiguration.ConfigLayers.Contains("__okconfig") || metaConfiguration.ConfigWriteLayer == "__okconfig")
                {
                    var okConfigLayer = await layerModel.GetLayer("__okconfig", mc, TimeThreshold.BuildLatest());
                    if (okConfigLayer == null)
                    {
                        await layerModel.UpsertLayer("__okconfig", mc);
                    }
                    mc.Commit();
                }
            }
        }

        private static async Task RebuildLatestTablesIfNonEmpty(IServiceScope scope)
        {
            var modelContextBuilder = scope.ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var partitionModel = scope.ServiceProvider.GetRequiredService<IPartitionModel>();
            var layerModel = scope.ServiceProvider.GetRequiredService<ILayerModel>();
            using (var mc = modelContextBuilder.BuildDeferred())
            {
                await RebuildLatestTablesService.RebuildLatestAttributesTable(true, partitionModel, layerModel, mc);
                await RebuildLatestTablesService.RebuildlatestRelationsTable(true, partitionModel, layerModel, mc);
                mc.Commit();
            }
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
