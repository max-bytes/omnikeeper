using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql.Logging;
using Omnikeeper.Base.Model;
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

            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                NpgsqlLogManager.Provider = scope.ServiceProvider.GetRequiredService<NpgsqlLoggingProvider>();

                // migration/rebuild of *-latest tables in database to be backward compatible
                await RebuildLatestTablesIfNonEmpty(scope);
            }

            // TODO: migration of old DB-table based base-configuration into meta-configuration and new trait-based base configuration
            // then deletion of the old DB based base-configuration
            //public async Task<BaseConfigurationV1> GetConfig(IModelContext trans)
            //{
            //    using var command = new NpgsqlCommand(@"
            //    SELECT config FROM config.general WHERE key = 'base' LIMIT 1
            //", trans.DBConnection, trans.DBTransaction);
            //    using var s = await command.ExecuteReaderAsync();

            //    if (!await s.ReadAsync())
            //        throw new Exception("Could not find base config");

            //    var configJO = s.GetFieldValue<JObject>(0);
            //    try
            //    {
            //        // NOTE: as soon as BaseConfigurationV2 comes along, we can first try to parse V2 here, then V1, and only then return null
            //        // we can also migrate from V1 to V2
            //        return BaseConfigurationV1.Serializer.Deserialize(configJO);
            //    }
            //    catch (Exception e)
            //    {
            //        logger.LogError(e, $"Could not deserialize application configuration");
            //        throw new Exception("Could not find base config", e);
            //    }
            //}

            host.Run();
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
