using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.Quartz;
using GraphQL;
using GraphQL.DataLoader;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Frameworks;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Controllers.OData;
using Omnikeeper.GraphQL;
using Omnikeeper.GraphQL.TraitEntities;
using Omnikeeper.GridView;
using Omnikeeper.Model;
using Omnikeeper.Model.Config;
using Omnikeeper.Model.Decorators;
using Omnikeeper.Runners;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using Omnikeeper.Utils.Decorators;
using Quartz;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

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
            //var prNaemon = new OKPluginNaemonConfig.PluginRegistration();
            //builder.Register<IPluginRegistration>(builder => prNaemon).SingleInstance();
            //var tmpServiceCollection = new ServiceCollection();
            //prNaemon.RegisterServices(tmpServiceCollection);
            //builder.Populate(tmpServiceCollection);

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
                        bool isMainPluginAssembly = false;
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

                                isMainPluginAssembly = true;
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

                        if (isMainPluginAssembly)
                            yield return assembly; // we only return those assemblies that contain an IPluginRegistration
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
            builder.RegisterType<IngestDataService>().SingleInstance();
            builder.RegisterType<ReactiveLogReceiver>().SingleInstance();

            builder.RegisterType<AuthRolePermissionChecker>().As<IAuthRolePermissionChecker>().SingleInstance();
            builder.RegisterType<CurrentUserAccessor>().As<ICurrentUserAccessor>().SingleInstance(); // TODO: remove, use ScopedLifetimeAccessor directly?
            builder.RegisterType<CurrentAuthorizedHttpUserService>().As<ICurrentUserService>().InstancePerLifetimeScope();

            builder.RegisterType<ScopedLifetimeAccessor>().SingleInstance();

            builder.RegisterType<DiffingCIService>().SingleInstance();

        }

        public static void RegisterLogging(ContainerBuilder builder)
        {
            builder.RegisterType<NpgsqlLoggingProvider>().SingleInstance();
        }

        public static void RegisterModels(ContainerBuilder builder, bool enablePerRequestModelCaching, bool enableOIA, bool enabledGenerators, bool enableUsageTracking)
        {
            builder.RegisterType<CIModel>().As<ICIModel>().SingleInstance();
            builder.RegisterType<CIIDModel>().As<ICIIDModel>().SingleInstance();
            builder.RegisterType<AttributeModel>().As<IAttributeModel>().SingleInstance();
            builder.RegisterType<BaseAttributeModel>().As<IBaseAttributeModel>().SingleInstance();
            builder.RegisterType<BaseAttributeRevisionistModel>().As<IBaseAttributeRevisionistModel>().SingleInstance();
            builder.RegisterType<BaseRelationRevisionistModel>().As<IBaseRelationRevisionistModel>().SingleInstance();
            builder.RegisterType<UserInDatabaseModel>().As<IUserInDatabaseModel>().SingleInstance();
            builder.RegisterType<LayerModel>().As<ILayerModel>().SingleInstance();
            builder.RegisterType<LayerDataModel>().As<ILayerDataModel>().SingleInstance();
            builder.RegisterType<LayerStatisticsModel>().As<ILayerStatisticsModel>().SingleInstance();
            builder.RegisterType<ChangesetStatisticsModel>().As<IChangesetStatisticsModel>().SingleInstance();
            builder.RegisterType<RelationModel>().As<IRelationModel>().SingleInstance();
            builder.RegisterType<BaseRelationModel>().As<IBaseRelationModel>().SingleInstance();
            builder.RegisterType<ChangesetModel>().As<IChangesetModel>().SingleInstance();
            builder.RegisterType<EffectiveTraitModel>().As<IEffectiveTraitModel>().SingleInstance();
            builder.RegisterType<BaseConfigurationModel>().As<IBaseConfigurationModel>().SingleInstance();
            builder.RegisterType<MetaConfigurationModel>().As<IMetaConfigurationModel>().SingleInstance();
            builder.RegisterType<OIAContextModel>().As<IOIAContextModel>().SingleInstance();
            builder.RegisterType<PartitionModel>().As<IPartitionModel>().SingleInstance();
            builder.RegisterType<GeneratorV1Model>().SingleInstance();
            builder.RegisterType<CLConfigV1Model>().SingleInstance();
            builder.RegisterType<AuthRoleModel>().SingleInstance();
            builder.RegisterType<PredicateModel>().SingleInstance();
            builder.RegisterType<RecursiveTraitModel>().SingleInstance();
            builder.RegisterType<GridViewContextModel>().SingleInstance();
            builder.RegisterType<InnerLayerDataModel>().SingleInstance();
            builder.RegisterType<ODataAPIContextModel>().SingleInstance();
            builder.RegisterType<LatestLayerChangeModel>().As<ILatestLayerChangeModel>().SingleInstance();
            builder.RegisterType<UsageStatsModel>().As<IUsageStatsModel>().SingleInstance();

            // these aren't real models, but we keep them here because they are closely related to models
            builder.RegisterType<TraitsProvider>().As<ITraitsProvider>().SingleInstance();
            builder.RegisterType<EffectiveGeneratorProvider>().As<IEffectiveGeneratorProvider>().SingleInstance();

            if (enablePerRequestModelCaching)
            {
                builder.RegisterType<PerRequestLayerCache>().InstancePerLifetimeScope();
                builder.RegisterDecorator<CachingLayerModel, ILayerModel>();
                builder.RegisterType<PerRequestMetaConfigurationCache>().InstancePerLifetimeScope();
                builder.RegisterDecorator<CachingMetaConfigurationModel, IMetaConfigurationModel>();
                builder.RegisterType<PerRequestTraitsProviderCache>().InstancePerLifetimeScope();
                builder.RegisterDecorator<CachingTraitsProvider, ITraitsProvider>();
            }

            // latest layer change caching
            // NOTE: removed because its ineffectual and does not work properly in HA scenario
            //builder.RegisterType<LatestLayerChangeCache>().SingleInstance();
            //builder.RegisterDecorator<CachingLatestLayerChangeModel, ILatestLayerChangeModel>();
            //builder.RegisterDecorator<CachingLatestLayerChangeAttributeModel, IBaseAttributeModel>();
            //builder.RegisterDecorator<CachingLatestLayerChangeRelationModel, IBaseRelationModel>();
            //builder.RegisterDecorator<CachingLatestLayerChangeLayerModel, ILayerModel>();

            // HACK: are defunct due to circular dependeny regarding LayerDataModel and OnlineAccessProxy
            //if (enableOIA)
            //{
            //    builder.RegisterDecorator<OIABaseAttributeModel, IBaseAttributeModel>();
            //    builder.RegisterDecorator<OIABaseRelationModel, IBaseRelationModel>();
            //}

            if (enabledGenerators)
            {
                builder.RegisterDecorator<GeneratingBaseAttributeModel, IBaseAttributeModel>();
            }

            if (enableUsageTracking)
            {
                builder.RegisterType<ScopedUsageTracker>().As<IScopedUsageTracker>().InstancePerLifetimeScope();
                builder.RegisterType<UsageDataAccumulator>().As<IUsageDataAccumulator>().SingleInstance();

                builder.RegisterDecorator<UsageTrackingEffectiveTraitModel, IEffectiveTraitModel>();
                builder.RegisterDecorator<UsageTrackingBaseAttributeModel, IBaseAttributeModel>();
                builder.RegisterDecorator<UsageTrackingBaseRelationModel, IBaseRelationModel>();
                builder.RegisterDecorator<UsageTrackingAuthRolePermissionChecker, IAuthRolePermissionChecker>();

                if (enabledGenerators)
                {
                    builder.RegisterDecorator<UsageTrackingEffectiveGeneratorProvider, IEffectiveGeneratorProvider>();
                }
            }
        }

        public static void RegisterOData(ContainerBuilder builder)
        {
            builder.RegisterType<EdmModelHolder>().SingleInstance();

            builder.RegisterType<MyODataRoutingApplicationModelProvider>().As<IApplicationModelProvider>().InstancePerDependency();
            builder.RegisterType<MyODataRoutingMatcherPolicy>().As<MatcherPolicy>().SingleInstance();
        }

        public static void RegisterGraphQL(ContainerBuilder builder)
        {
            builder.RegisterType<GraphQLSchemaHolder>().SingleInstance();
            builder.RegisterType<MyDocumentExecutor>().As<IDocumentExecuter>().SingleInstance(); // custom document executor that does serial queries, required by postgres
            builder.RegisterType<DataLoaderContextAccessor>().As<IDataLoaderContextAccessor>().SingleInstance();
            builder.RegisterType<DataLoaderDocumentListener>().SingleInstance();
            builder.RegisterType<DataLoaderService>().As<IDataLoaderService>().SingleInstance();

            builder.RegisterType<TraitEntitiesQuerySchemaLoader>().SingleInstance();
            builder.RegisterType<TraitEntitiesMutationSchemaLoader>().SingleInstance();
            builder.RegisterType<TypeContainerCreator>().SingleInstance();
        }

        public static void RegisterQuartz(ContainerBuilder builder, string connectionString, string distributedQuartzInstanceID)
        {
            var localSchedulerConfig = new NameValueCollection {
                {"quartz.threadPool.threadCount", "3" },
                {"quartz.scheduler.threadName", "Scheduler" },
                {"quartz.jobStore.type","Quartz.Simpl.RAMJobStore, Quartz" },
                {"quartz.scheduler.instanceName", "local" }
            };

            var distributedSchedulerConfig = new NameValueCollection {
                {"quartz.threadPool.threadCount", "3" },
                {"quartz.scheduler.threadName", "Scheduler" },

                {"quartz.dataSource.myDS.provider","Npgsql" },
                {"quartz.dataSource.myDS.connectionString", connectionString },

                {"quartz.jobStore.dataSource", "myDS" },
                {"quartz.jobStore.type","Quartz.Impl.AdoJobStore.JobStoreTX, Quartz" },
                {"quartz.jobStore.driverDelegateType", "Quartz.Impl.AdoJobStore.PostgreSQLDelegate, Quartz" },
                {"quartz.jobStore.tablePrefix","qrtz." },
                {"quartz.jobStore.useProperties","true" },
                {"quartz.serializer.type","json" },

                {"quartz.jobStore.clustered", "true" },
                {"quartz.scheduler.instanceId", distributedQuartzInstanceID },
                {"quartz.scheduler.instanceName", "distributed" }
            };

            builder.Register(c => new AutofacJobFactory(c.Resolve<ILifetimeScope>(), "quartz.job", null))
                .AsSelf()
                .As<IJobFactory>()
                .SingleInstance();

            builder.Register<ISchedulerFactory>(c =>
            {
                var autofacSchedulerFactory = new AutofacSchedulerFactory(localSchedulerConfig, c.Resolve<AutofacJobFactory>());
                return autofacSchedulerFactory;
            })
                .Named<ISchedulerFactory>("local")
                .SingleInstance();
            builder.Register<IScheduler>(c =>
            {
                var factory = c.ResolveNamed<ISchedulerFactory>("local");
                var s = factory.GetScheduler().ConfigureAwait(false).GetAwaiter().GetResult();
                return s;
            })
                .Keyed<IScheduler>("localScheduler")
                .SingleInstance();


            builder.Register<ISchedulerFactory>(c =>
            {
                var autofacSchedulerFactory = new AutofacSchedulerFactory(distributedSchedulerConfig, c.Resolve<AutofacJobFactory>());
                return autofacSchedulerFactory;
            })
                .Named<ISchedulerFactory>("distributed")
                .SingleInstance();
            builder.Register<IScheduler>(c =>
            {
                var factory = c.ResolveNamed<ISchedulerFactory>("distributed");
                var s = factory.GetScheduler().ConfigureAwait(false).GetAwaiter().GetResult();
                return s;
            })
                .Keyed<IScheduler>("distributedScheduler")
                .SingleInstance();

            // jobs registration
            builder.RegisterModule(new QuartzAutofacJobsModule(Assembly.GetExecutingAssembly()) { });

            // jobs
            builder.RegisterType<CLBJob>().InstancePerLifetimeScope();
            builder.RegisterType<CLBSingleJob>().InstancePerLifetimeScope();
            builder.RegisterType<CLBLastRunCache>().SingleInstance();
            builder.RegisterType<CLBProcessedChangesetsCache>().SingleInstance();
            builder.RegisterType<ArchiveOldDataJob>().InstancePerLifetimeScope();
            builder.RegisterType<ExternalIDManagerJob>().InstancePerLifetimeScope();
            builder.RegisterType<MarkedForDeletionJob>().InstancePerLifetimeScope();
            builder.RegisterType<UsageDataWriterJob>().InstancePerLifetimeScope();
            builder.RegisterType<GraphQLSchemaReloaderJob>().InstancePerLifetimeScope();
            builder.RegisterType<EdmModelReloaderJob>().InstancePerLifetimeScope();
        }
    }
}
