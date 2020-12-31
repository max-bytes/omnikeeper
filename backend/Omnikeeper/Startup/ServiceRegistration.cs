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
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
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
        public static void RegisterDB(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<DBConnectionBuilder>();
            services.AddScoped((sp) =>
            {
                var dbcb = sp.GetRequiredService<DBConnectionBuilder>();
                return dbcb.Build(configuration);
            });
            services.AddScoped<IModelContextBuilder, ModelContextBuilder>();
        }
        public static void RegisterDB(IServiceCollection services, string dbName, bool pooling, bool reloadTypes)
        {
            services.AddSingleton<DBConnectionBuilder>();
            services.AddScoped((sp) =>
            {
                var dbcb = sp.GetRequiredService<DBConnectionBuilder>();
                return dbcb.Build(dbName, pooling, reloadTypes);
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
                    foreach (var e in dllEntries)
                    {
                        var finalDLLFile = Path.Combine(extractedFolder, e.Name);
                        e.ExtractToFile(finalDLLFile, overwrite: true);

                        Assembly? assembly;
                        try
                        {
                            PluginLoadContext loadContext = new PluginLoadContext(finalDLLFile);
                            assembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(finalDLLFile)));
                            services.Scan(scan => 
                                scan.FromAssemblies(assembly)
                                    .AddClasses()
                                    .AsSelfWithInterfaces() // see https://andrewlock.net/using-scrutor-to-automatically-register-your-services-with-the-asp-net-core-di-container/#registering-an-implementation-using-forwarded-services
                                    .WithSingletonLifetime()
                            );

                            var assemblyName = assembly.GetName();
                            if (assemblyName == null)
                                throw new Exception("Assembly without name encountered");
                            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                            var lp = new LoadedPlugin(
                                    assemblyName.Name ?? "Unknown Plugin",
                                    assemblyName.Version ?? new Version(0, 0, 0),
                                    informationalVersion ?? "Unknown version");
                            services.AddSingleton<ILoadedPlugin>(lp);

                            Console.WriteLine($"Loaded OKPlugin {lp.Name}, Version {lp.Version}"); // TODO: better logging
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
            services.AddSingleton<IRecursiveTraitModel, RecursiveTraitModel>();
            services.AddSingleton<IEffectiveTraitModel, EffectiveTraitModel>();
            services.AddSingleton<IBaseConfigurationModel, BaseConfigurationModel>();
            services.AddSingleton<IOIAContextModel, OIAContextModel>();
            services.AddSingleton<IGridViewContextModel, GridViewContextModel>();
            services.AddSingleton<IPartitionModel, PartitionModel>();

            // these aren't real models, but we keep them here because they are closely related to models
            services.AddSingleton<ITraitsProvider, TraitsProvider>();
            services.AddSingleton<ITemplatesProvider, TemplatesProvider>();
            services.AddSingleton<IEffectiveGeneratorProvider, EffectiveGeneratorProvider>();

            if (enableModelCaching)
            {
                services.Decorate<IBaseAttributeModel, CachingBaseAttributeModel>();
                services.Decorate<IBaseAttributeRevisionistModel, CachingBaseAttributeRevisionistModel>();
                services.Decorate<ILayerModel, CachingLayerModel>();
                services.Decorate<IBaseRelationModel, CachingBaseRelationModel>();
                services.Decorate<IBaseRelationRevisionistModel, CachingBaseRelationRevisionistModel>();
                services.Decorate<IPredicateModel, CachingPredicateModel>();
                services.Decorate<IODataAPIContextModel, CachingODataAPIContextModel>();
                services.Decorate<IRecursiveTraitModel, CachingRecursiveTraitModel>();
                services.Decorate<IBaseConfigurationModel, CachingBaseConfigurationModel>();

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
