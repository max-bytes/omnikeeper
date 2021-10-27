using GraphQL;
using GraphQL.Types;
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
using GraphQL.DataLoader;
using Omnikeeper.Base.Entity;
using Omnikeeper.GridView.Entity;
using Autofac;
using Autofac.Extensions.DependencyInjection;

namespace Omnikeeper.Startup
{
    public static class ServiceRegistration
    {
        public static void RegisterDB(ContainerBuilder builder, string connectionString, bool reloadTypes)
        {
            builder.RegisterType<DBConnectionBuilder>().SingleInstance();
            builder.Register(c =>
            {
                var dbcb = c.Resolve<DBConnectionBuilder>();
                return dbcb.BuildFromConnectionString(connectionString, reloadTypes);
            }).InstancePerLifetimeScope();
            builder.RegisterType<ModelContextBuilder>().As<IModelContextBuilder>().InstancePerLifetimeScope();
        }

        public static void RegisterOIABase(ContainerBuilder builder)
        {
            builder.RegisterType<OnlineAccessProxy>().As<IOnlineAccessProxy>().SingleInstance();
            builder.RegisterType<ExternalIDMapper>().As<IExternalIDMapper>().SingleInstance();
            builder.RegisterType<ExternalIDMapPostgresPersister>().As<IExternalIDMapPersister>().SingleInstance();
            builder.RegisterType<InboundAdapterManager>().As<IInboundAdapterManager>().SingleInstance();
        }

        public static IEnumerable<Assembly> RegisterOKPlugins(ContainerBuilder builder, string pluginFolder)
        {
            //services.AddSingleton<OKPluginGenericJSONIngest.IContextModel, OKPluginGenericJSONIngest.ContextModel>();
            //services.AddTransient<Controllers.Ingest.PassiveFilesController>();
            //services.AddTransient<Controllers.Ingest.ManageContextController>();
            //var pr = new OKPluginGenericJSONIngest.PluginRegistration();
            //pr.RegisterServices(services);
            //var cs = Configuration.GetConnectionString("OmnikeeperDatabaseConnection");
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

                            // we use a temporary container to extract the plugin registration, which then does the actual registration
                            var tmpBuilder = new ContainerBuilder();
                            tmpBuilder.RegisterAssemblyTypes(assembly).Where(t => t.IsAssignableTo<IPluginRegistration>()).AsImplementedInterfaces().SingleInstance();
                            using var tmpContainer = tmpBuilder.Build();
                            var x = tmpContainer.ComponentRegistry.Registrations;
                            if (tmpContainer.TryResolve<IPluginRegistration>(out var pr))
                            {
                                // register plugin itself and its own services
                                builder.RegisterInstance(pr);
                                var serviceCollection = new ServiceCollection();
                                pr.RegisterServices(serviceCollection);
                                builder.Populate(serviceCollection);

                                Console.WriteLine($"Loaded OKPlugin {pr.Name}, Version {pr.Version}"); // TODO: better logging
                            }
                            else
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

        public static void RegisterServices(ContainerBuilder builder)
        {
            builder.RegisterType<CIMappingService>().SingleInstance();
            builder.RegisterType<ManagementAuthorizationService>().As<IManagementAuthorizationService>().SingleInstance();
            builder.RegisterType<LayerBasedAuthorizationService>().As<ILayerBasedAuthorizationService>().SingleInstance();
            builder.RegisterType<CIBasedAuthorizationService>().As<ICIBasedAuthorizationService>().SingleInstance();
            builder.RegisterType<DataPartitionService>().As<IDataPartitionService>().SingleInstance();
            builder.RegisterType<MarkedForDeletionService>().SingleInstance();
            builder.RegisterType<IngestDataService>().InstancePerLifetimeScope(); // TODO: make singleton
            builder.RegisterType<CurrentUserService>().As<ICurrentUserService>().InstancePerLifetimeScope();
            builder.RegisterType<ReactiveLogReceiver>().SingleInstance();
        }

        public static void RegisterLogging(ContainerBuilder builder)
        {
            builder.RegisterType<NpgsqlLoggingProvider>().SingleInstance();
        }

        public static void RegisterModels(ContainerBuilder builder, bool enableModelCaching, bool enableEffectiveTraitCaching, bool enableOIA, bool enabledGenerators)
        {
            builder.RegisterType<CISearchModel>().As<ICISearchModel>().SingleInstance();
            builder.RegisterType<CIModel>().As<ICIModel>().SingleInstance();
            builder.RegisterType<CIIDModel>().As<ICIIDModel>().SingleInstance();
            builder.RegisterType<AttributeModel>().As<IAttributeModel>().SingleInstance();
            builder.RegisterType<BaseAttributeModel>().As<IBaseAttributeModel>().SingleInstance();
            builder.RegisterType<BaseAttributeRevisionistModel>().As<IBaseAttributeRevisionistModel>().SingleInstance();
            builder.RegisterType<BaseRelationRevisionistModel>().As<IBaseRelationRevisionistModel>().SingleInstance();
            builder.RegisterType<UserInDatabaseModel>().As<IUserInDatabaseModel>().SingleInstance();
            builder.RegisterType<LayerModel>().As<ILayerModel>().SingleInstance();
            builder.RegisterType<LayerStatisticsModel>().As<ILayerStatisticsModel>().SingleInstance();
            builder.RegisterType<ChangesetStatisticsModel>().As<IChangesetStatisticsModel>().SingleInstance();
            builder.RegisterType<RelationModel>().As<IRelationModel>().SingleInstance();
            builder.RegisterType<BaseRelationModel>().As<IBaseRelationModel>().SingleInstance();
            builder.RegisterType<ChangesetModel>().As<IChangesetModel>().SingleInstance();
            builder.RegisterType<CacheModel>().As<ICacheModel>().SingleInstance();
            builder.RegisterType<ODataAPIContextModel>().As<IODataAPIContextModel>().SingleInstance();
            builder.RegisterType<EffectiveTraitModel>().As<IEffectiveTraitModel>().SingleInstance();
            builder.RegisterType<BaseConfigurationModel>().As<IBaseConfigurationModel>().SingleInstance();
            builder.RegisterType<MetaConfigurationModel>().As<IMetaConfigurationModel>().SingleInstance();
            builder.RegisterType<OIAContextModel>().As<IOIAContextModel>().SingleInstance();
            builder.RegisterType<PartitionModel>().As<IPartitionModel>().SingleInstance();
            builder.RegisterType<GenericTraitEntityModel<GeneratorV1, string>>().SingleInstance(); // TODO: ok this way?
            builder.RegisterType<GenericTraitEntityModel<CLConfigV1, string>>().SingleInstance(); // TODO: ok this way?
            builder.RegisterType<GenericTraitEntityModel<AuthRole, string>>().SingleInstance(); // TODO: ok this way?
            builder.RegisterType<GenericTraitEntityModel<Predicate, string>>().SingleInstance(); // TODO: ok this way?
            builder.RegisterType<GenericTraitEntityModel<RecursiveTrait, string>>().SingleInstance(); // TODO: ok this way?
            builder.RegisterType<GenericTraitEntityModel<GridViewContext, string>>().SingleInstance(); // TODO: ok this way?

            // these aren't real models, but we keep them here because they are closely related to models
            builder.RegisterType<TraitsProvider>().As<ITraitsProvider>().SingleInstance();
            builder.RegisterType<EffectiveGeneratorProvider>().As<IEffectiveGeneratorProvider>().SingleInstance();
            builder.RegisterType<ProtoBufDataSerializer>().As<IDataSerializer>().SingleInstance();

            if (enableModelCaching)
            {
                builder.RegisterDecorator<CachingLayerModel, ILayerModel>();
                builder.RegisterDecorator<CachingODataAPIContextModel, IODataAPIContextModel>();
                builder.RegisterDecorator<CachingBaseConfigurationModel, IBaseConfigurationModel>();
                builder.RegisterDecorator<CachingMetaConfigurationModel, IMetaConfigurationModel>();
                builder.RegisterDecorator<CachingPartitionModel, IPartitionModel>();
            }

            // TODO: rework or remove
            if (enableEffectiveTraitCaching)
            {
                //services.Decorate<IEffectiveTraitModel, CachingEffectiveTraitModel>();
                //services.Decorate<IBaseAttributeModel, TraitCacheInvalidationBaseAttributeModel>();
                //services.Decorate<IBaseAttributeRevisionistModel, TraitCacheInvalidationBaseAttributeRevisionistModel>();
                //services.Decorate<IBaseRelationModel, TraitCacheInvalidationBaseRelationModel>();
                //services.Decorate<IBaseRelationRevisionistModel, TraitCacheInvalidationBaseRelationRevisionistModel>();
                //services.AddSingleton<EffectiveTraitCache>(); // TODO: create interface
            }

            if (enableOIA)
            {
                builder.RegisterDecorator<OIABaseAttributeModel, IBaseAttributeModel>();
                builder.RegisterDecorator<OIABaseRelationModel, IBaseRelationModel>();
            }

            if (enabledGenerators)
            {
                builder.RegisterDecorator<GeneratingBaseAttributeModel, IBaseAttributeModel>();
            }
        }

        public static void RegisterGraphQL(ContainerBuilder builder)
        {
            builder.RegisterType<GraphQLSchema>().As<ISchema>().SingleInstance();
            builder.RegisterType<MyDocumentExecutor>().As<IDocumentExecuter>().SingleInstance(); // custom document executor that does serial queries, required by postgres
            builder.RegisterType<SpanJSONDocumentWriter>().As<IDocumentWriter>().SingleInstance();
            builder.RegisterType<DataLoaderContextAccessor>().As<IDataLoaderContextAccessor>().SingleInstance();
            builder.RegisterType<DataLoaderDocumentListener>().SingleInstance();

            // required for Autofac
            builder
              .Register(c => new FuncServiceProvider(c.Resolve<IComponentContext>().Resolve))
              .As<IServiceProvider>()
              .InstancePerDependency();
        }
    }
}
