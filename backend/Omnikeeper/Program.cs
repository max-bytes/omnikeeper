using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Npgsql.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
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

                // migrate layer data into proper layer-data entities
                await MigrateLayerData(scope);
            }

            host.Run();
        }

        // moves data from the old layer_* tables into the new CI-based layer-data structure
        // truncates the old layer_* tables afterwards
        // NOTE, TODO: at a later stage, we should delete the outdated and empty tables, once every instance is migrated
        // we should do this with a DB migration; after this is done, we can remove this migration script
        private static async Task MigrateLayerData(IServiceScope scope)
        {
            var modelContextBuilder = scope.ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var metaConfigurationModel = scope.ServiceProvider.GetRequiredService<IMetaConfigurationModel>();
            var changesetModel = scope.ServiceProvider.GetRequiredService<IChangesetModel>();
            var layerDataModel = scope.ServiceProvider.GetRequiredService<ILayerDataModel>();
            var userModel = scope.ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            using (var trans = modelContextBuilder.BuildDeferred())
            {
                try
                {
                    var timeThreshold = TimeThreshold.BuildLatest();

                    // bail if all layer_* tables are empty anyway
                    using (var command = new NpgsqlCommand($@"
                        SELECT CASE 
                            WHEN 
                                EXISTS (SELECT * FROM layer_state LIMIT 1) OR 
                                EXISTS (SELECT * FROM layer_computelayerbrain LIMIT 1) OR
                                EXISTS (SELECT * FROM layer_onlineinboundlayerplugin LIMIT 1) OR
                                EXISTS (SELECT * FROM layer_color LIMIT 1) OR
                                EXISTS (SELECT * FROM layer_generators LIMIT 1)
                                    THEN 1
                            ELSE 0 
                        END", trans.DBConnection, trans.DBTransaction))
                    {
                        var hasDataInt = (int?)await command.ExecuteScalarAsync();
                        if (hasDataInt.HasValue && hasDataInt.Value == 0L)
                            return;
                    }

                    var l = new List<(string id, AnchorState state, string clConfig, string oilp, Color color, string[] generators)>();
                    using (var command = new NpgsqlCommand($@"SELECT l.id, ls.state, lclb.brainname, loilp.pluginname, lc.color, lg.generators FROM layer l
                        LEFT JOIN 
                            (SELECT DISTINCT ON (layer_id) layer_id, state FROM layer_state WHERE timestamp <= @time_threshold ORDER BY layer_id, timestamp DESC NULLS LAST) ls
                            ON ls.layer_id = l.id
                        LEFT JOIN 
                            (SELECT DISTINCT ON (layer_id) layer_id, brainname FROM layer_computelayerbrain WHERE timestamp <= @time_threshold ORDER BY layer_id, timestamp DESC NULLS LAST) lclb
                            ON lclb.layer_id = l.id
                        LEFT JOIN 
                            (SELECT DISTINCT ON (layer_id) layer_id, pluginname FROM layer_onlineinboundlayerplugin WHERE timestamp <= @time_threshold ORDER BY layer_id, timestamp DESC NULLS LAST) loilp
                            ON loilp.layer_id = l.id
                        LEFT JOIN 
                            (SELECT DISTINCT ON (layer_id) layer_id, color FROM layer_color WHERE timestamp <= @time_threshold ORDER BY layer_id, timestamp DESC NULLS LAST) lc
                            ON lc.layer_id = l.id
                        LEFT JOIN 
                            (SELECT DISTINCT ON (layer_id) layer_id, generators FROM layer_generators WHERE timestamp <= @time_threshold ORDER BY layer_id, timestamp DESC NULLS LAST) lg
                            ON lg.layer_id = l.id", trans.DBConnection, trans.DBTransaction))
                    {
                        command.Parameters.AddWithValue("time_threshold", timeThreshold.Time);
                        using var r = await command.ExecuteReaderAsync();

                        while (await r.ReadAsync())
                        {
                            var id = r.GetString(0);
                            var state = (r.IsDBNull(1)) ? ILayerDataModel.DefaultState : r.GetFieldValue<AnchorState>(1);
                            var clConfig = (r.IsDBNull(2)) ? "" : r.GetString(2);
                            var oilp = (r.IsDBNull(3)) ? "" : r.GetString(3);
                            var color = (r.IsDBNull(4)) ? ILayerDataModel.DefaultColor : Color.FromArgb(r.GetInt32(4));
                            var generators = (r.IsDBNull(5)) ? Array.Empty<string>() : r.GetFieldValue<string[]>(5);
                            generators = generators.Where(g => !string.IsNullOrEmpty(g)).ToArray();
                            l.Add((id, state, clConfig, oilp, color, generators));
                        }
                    }

                    // upsert user
                    var username = $"__migrate_layer_data";
                    var displayName = username;
                    var userGuid = new Guid("2544f9a7-cc17-4cba-8052-e88656cf1ef3");
                    var user = await userModel.UpsertUser(username, displayName, userGuid, UserType.Robot, trans);

                    var changesetProxy = new ChangesetProxy(user, timeThreshold, changesetModel);
                    foreach (var (id, state, clConfig, oilp, color, generators) in l)
                    {
                        await layerDataModel.UpsertLayerData(id, "", color.ToArgb(), state.ToString(), clConfig, oilp, generators, new DataOriginV1(DataOriginType.Manual), changesetProxy, trans);
                    }

                    // empty the tables
                    using (var command = new NpgsqlCommand($@"
                        TRUNCATE TABLE layer_state;
                        TRUNCATE TABLE layer_computelayerbrain;
                        TRUNCATE TABLE layer_onlineinboundlayerplugin;
                        TRUNCATE TABLE layer_color;
                        TRUNCATE TABLE layer_generators;", trans.DBConnection, trans.DBTransaction))
                    {
                        await command.ExecuteNonQueryAsync();
                    }

                    trans.Commit();
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Encountered error while migration layer data");
                }
            }
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
                    await layerModel.CreateLayerIfNotExists("__okconfig", mc);
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
