using GraphQL;
using GraphQL.NewtonsoftJson;
using GraphQL.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Frameworks;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Base.Utils.Serialization;
using Omnikeeper.GraphQL;
using Omnikeeper.GridView.Model;
using Omnikeeper.Model;
using Omnikeeper.Model.Config;
using Omnikeeper.Model.Decorators;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

namespace Omnikeeper.Startup
{
    public static class ServiceRegistration
    {
        public static void RegisterDB(IServiceCollection services, string connectionString, bool reloadTypes)
        {
            services.AddSingleton<DBConnectionBuilder>();
            services.AddScoped((sp) =>
            {
                var dbcb = sp.GetRequiredService<DBConnectionBuilder>();
                return dbcb.BuildFromConnectionString(connectionString, reloadTypes);
            });
            services.AddScoped<IModelContextBuilder, ModelContextBuilder>();
        }

        public static void RegisterOIABase(IServiceCollection services)
        {
            services.AddSingleton<IOnlineAccessProxy, OnlineAccessProxy>();
            services.AddSingleton<IExternalIDMapper, ExternalIDMapper>();
            services.AddSingleton<IExternalIDMapPersister, ExternalIDMapPostgresPersister>();
            services.AddSingleton<IInboundAdapterManager, InboundAdapterManager>();
        }

        public static IEnumerable<Assembly> RegisterOKPlugins(IServiceCollection services, string pluginFolder)
        {
            //services.AddSingleton<OKPluginGenericJSONIngest.IContextModel, OKPluginGenericJSONIngest.ContextModel>();
            //services.AddTransient<Controllers.Ingest.PassiveFilesController>();
            //services.AddTransient<Controllers.Ingest.ManageContextController>();
            //var pr = new OKPluginGenericJSONIngest.PluginRegistration();
            //pr.RegisterServices(services);
            //var cs = Configuration.GetConnectionString("LandscapeDatabaseConnection");
            //var result = plugin.DBMigration.Migrate(cs);


            var dotNetFramework = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            var frameworkNameProvider = new FrameworkNameProvider(
                new[] { DefaultFrameworkMappings.Instance },
                new[] { DefaultPortableFrameworkMappings.Instance });
            var nuGetFramework = NuGetFramework.ParseFrameworkName(dotNetFramework, frameworkNameProvider);

            // load plugins from directory
            var extractedFolder = Path.Combine(pluginFolder, "extracted");
            var di = Directory.CreateDirectory(extractedFolder);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (var file in Directory.GetFiles(pluginFolder, "*.nupkg", SearchOption.AllDirectories))
            {
                using var fs = File.OpenRead(file);
                using var archive = new ZipArchive(fs);
                var zipArchiveEntries = archive.Entries
                    .Where(e => e.Name.EndsWith(".dll")).ToList();

                var entriesWithTargetFramework = zipArchiveEntries
                    .Select(e => new
                    {
                        TargetFramework = NuGetFramework.Parse(e.FullName.Split('/')[1]),
                        Entry = e
                    }).ToList();

                var matchingEntries = entriesWithTargetFramework
                    .Where(e => e.TargetFramework.Version.Major > 0 &&
                                e.TargetFramework.Version <= nuGetFramework.Version).ToList();

                var orderedEntries = matchingEntries
                    .OrderBy(e => e.TargetFramework.GetShortFolderName()).ToList();

                if (orderedEntries.Any())
                {
                    var dllEntries = orderedEntries
                        .GroupBy(e => e.TargetFramework.GetShortFolderName())
                        .Last()
                        .Select(e => e.Entry)
                        .ToArray();

                    var pluginAssemblies = new List<string>();
                    PluginLoadContext loadContext = new PluginLoadContext();
                    foreach (var e in dllEntries)
                    {
                        var finalDLLFile = Path.Combine(extractedFolder, e.Name);
                        e.ExtractToFile(finalDLLFile, overwrite: true);

                        Assembly? assembly;
                        try
                        {
                            loadContext.AddResolverFromPath(finalDLLFile);
                            assembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(finalDLLFile)));

                            // we use a temporary service collection and service provider to extract the plugin registration, which then does the actual registration
                            var tmpSC = new ServiceCollection();
                            tmpSC.Scan(scan =>
                                scan.FromAssemblies(assembly)
                                    .AddClasses(classes => classes.AssignableTo<IPluginRegistration>())
                                    .AsSelfWithInterfaces()
                                    .WithSingletonLifetime()
                            );
                            var tmpSP = tmpSC.BuildServiceProvider();
                            var pr = tmpSP.GetService<IPluginRegistration>();
                            if (pr != null)
                            {
                                // register plugin itself and its own services
                                services.AddSingleton(pr);
                                pr.RegisterServices(services);
                                Console.WriteLine($"Loaded OKPlugin {pr.Name}, Version {pr.Version}"); // TODO: better logging
                            } else
                            {
                                Console.WriteLine($"Encountered OKPlugin without IPluginRegistration! Assembly: {assembly.FullName}"); // TODO: better logging
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Could not load assembly at location {finalDLLFile}: {ex.Message}"); // TODO: better error handling
                            continue;
                        }
                        yield return assembly;
                    }
                }
            }
        }

        public static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<CIMappingService, CIMappingService>();
            services.AddSingleton<IManagementAuthorizationService, ManagementAuthorizationService>();
            services.AddSingleton<ILayerBasedAuthorizationService, LayerBasedAuthorizationService>();
            services.AddSingleton<ICIBasedAuthorizationService, CIBasedAuthorizationService>();
            services.AddSingleton<IDataPartitionService, DataPartitionService>();
            services.AddSingleton<MarkedForDeletionService>();
            services.AddScoped<IngestDataService>(); // TODO: make singleton
            services.AddSingleton<IPredicateWriteService, PredicateWriteService>();
            services.AddSingleton<IRecursiveTraitWriteService, RecursiveTraitWriteService>();

            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddSingleton<ReactiveLogReceiver>();
        }

        public static void RegisterLogging(IServiceCollection services)
        {
            services.AddSingleton<NpgsqlLoggingProvider>();
        }

        public static void RegisterModels(IServiceCollection services, bool enableModelCaching, bool enableOIA, bool enabledGenerators)
        {
            services.AddSingleton<ICISearchModel, CISearchModel>();
            services.AddSingleton<ICIModel, CIModel>();
            services.AddSingleton<ICIIDModel, CIIDModel>();
            services.AddSingleton<IAttributeModel, AttributeModel>();
            services.AddSingleton<IBaseAttributeModel, BaseAttributeModel>();
            services.AddSingleton<IBaseAttributeRevisionistModel, BaseAttributeRevisionistModel>();
            services.AddSingleton<IBaseRelationRevisionistModel, BaseRelationRevisionistModel>();
            services.AddSingleton<IUserInDatabaseModel, UserInDatabaseModel>();
            services.AddSingleton<ILayerModel, LayerModel>();
            services.AddSingleton<ILayerStatisticsModel, LayerStatisticsModel>();
            services.AddSingleton<IRelationModel, RelationModel>();
            services.AddSingleton<IBaseRelationModel, BaseRelationModel>();
            services.AddSingleton<IChangesetModel, ChangesetModel>();
            services.AddSingleton<ITemplateModel, TemplateModel>();
            services.AddSingleton<IPredicateModel, PredicateModel>();
            services.AddSingleton<ICacheModel, CacheModel>();
            services.AddSingleton<IODataAPIContextModel, ODataAPIContextModel>();
            services.AddSingleton<IRecursiveDataTraitModel, RecursiveDataTraitModel>();
            services.AddSingleton<IEffectiveTraitModel, EffectiveTraitModel>();
            services.AddSingleton<IBaseConfigurationModel, BaseConfigurationModel>();
            services.AddSingleton<IOIAContextModel, OIAContextModel>();
            services.AddSingleton<IGridViewContextModel, GridViewContextModel>();
            services.AddSingleton<IPartitionModel, PartitionModel>();

            // these aren't real models, but we keep them here because they are closely related to models
            services.AddSingleton<ITraitsProvider, TraitsProvider>();
            services.AddSingleton<ITemplatesProvider, TemplatesProvider>();
            services.AddSingleton<IEffectiveGeneratorProvider, EffectiveGeneratorProvider>();
            services.AddSingleton<IDataSerializer, ProtoBufDataSerializer>();

            if (enableModelCaching)
            {
                services.Decorate<IBaseAttributeModel, CachingBaseAttributeModel>();
                services.Decorate<IBaseAttributeRevisionistModel, CachingBaseAttributeRevisionistModel>();
                services.Decorate<ILayerModel, CachingLayerModel>();
                services.Decorate<IBaseRelationModel, CachingBaseRelationModel>();
                services.Decorate<IBaseRelationRevisionistModel, CachingBaseRelationRevisionistModel>();
                services.Decorate<IODataAPIContextModel, CachingODataAPIContextModel>();
                services.Decorate<IBaseConfigurationModel, CachingBaseConfigurationModel>();
                services.Decorate<IPartitionModel, CachingPartitionModel>();

                services.Decorate<ITemplatesProvider, CachedTemplatesProvider>();
            }

            if (enableOIA)
            {
                services.Decorate<IBaseAttributeModel, OIABaseAttributeModel>();
                services.Decorate<IBaseRelationModel, OIABaseRelationModel>();
            }

            if (enabledGenerators)
            {
                services.Decorate<IBaseAttributeModel, GeneratingBaseAttributeModel>();
            }
        }

        public static void RegisterGraphQL(IServiceCollection services)
        {
            services.AddSingleton<ISchema, GraphQLSchema>();
            services.AddSingleton<IDocumentExecuter, MyDocumentExecutor>(); // custom document executor that does serial queries, required by postgres
            services.AddSingleton<IDocumentWriter, DocumentWriter>();
        }
    }
}
