using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Controllers.OData;
using Omnikeeper.GraphQL;
using Omnikeeper.Service;
using Omnikeeper.Startup;
using Serilog;
using Serilog.Events;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var version = VersionService.GetVersion();
            Console.WriteLine($"Running version: {version}");

            try
            {
                var host = Host.CreateDefaultBuilder(args)
                    .UseSerilog((hostingContext, service, loggerConfig) =>
                    {
                        // configure logging
                        loggerConfig
                        .ReadFrom.Configuration(hostingContext.Configuration)

                        // add SignalR sink in code
                        .WriteTo.Sink(
                            new MySignalRSink<SignalRHubLogging, ILogHub>(null, false, service, new string[0], new string[0], new string[0]),
                            LogEventLevel.Information
                        );
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
                        services.AddHostedService<QuartzJobStarter>();
                    })
                    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                    .Build();

                // set logging provider before doing any Npsql operations
                using (var scope = host.Services.CreateScope())
                {
                    NpgsqlLogManager.Provider = scope.ServiceProvider.GetRequiredService<NpgsqlLoggingProvider>();
                }

                using (var scope = host.Services.CreateScope())
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    try
                    {
                        var traitsProvider = scope.ServiceProvider.GetRequiredService<ITraitsProvider>();
                        var modelContextBuilder = scope.ServiceProvider.GetRequiredService<IModelContextBuilder>();
                        var timeThreshold = TimeThreshold.BuildLatest();
                        var trans = modelContextBuilder.BuildImmediate();
                        var activeTraits = await traitsProvider.GetActiveTraits(trans, timeThreshold);

                        var graphqlSchemaHolder = scope.ServiceProvider.GetRequiredService<GraphQLSchemaHolder>();
                        graphqlSchemaHolder.ReInitSchema(scope.ServiceProvider, activeTraits, logger);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Encountered error while trying to initialize GraphQL schema");
                    }
                }

                using (var scope = host.Services.CreateScope())
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    try
                    {
                        var traitsProvider = scope.ServiceProvider.GetRequiredService<ITraitsProvider>();
                        var modelContextBuilder = scope.ServiceProvider.GetRequiredService<IModelContextBuilder>();
                        var timeThreshold = TimeThreshold.BuildLatest();
                        var trans = modelContextBuilder.BuildImmediate();
                        var activeTraits = await traitsProvider.GetActiveTraits(trans, timeThreshold);

                        var edmModelHolder = scope.ServiceProvider.GetRequiredService<EdmModelHolder>();
                        edmModelHolder.ReInitModel(scope.ServiceProvider, activeTraits, logger);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Encountered error while trying to initialize Edm model");
                    }
                }

                using (var scope = host.Services.CreateScope())
                {
                    await CreateMetaLayersIfNotExists(scope);
                }

                host.Run();
            } catch(Exception e)
            {
                Console.WriteLine($"Host terminated unexpectedly: {e.Message}");
            }
        }

        private static async Task CreateMetaLayersIfNotExists(IServiceScope scope)
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
            using (var mc = modelContextBuilder.BuildDeferred())
            {
                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(mc);
                if (metaConfiguration.IssueLayers.Contains("__okissues") || metaConfiguration.IssueWriteLayer == "__okissues")
                {
                    await layerModel.CreateLayerIfNotExists("__okissues", mc);
                    mc.Commit();
                }
            }
        }
    }
}
