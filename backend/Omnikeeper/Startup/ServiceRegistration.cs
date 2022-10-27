using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.Quartz;
using GraphQL;
using GraphQL.DataLoader;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Frameworks;
using Omnikeeper.Authz;
using Omnikeeper.Base.Authz;
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
            var files = Directory.GetFiles(pluginFolder, "*.nupkg", SearchOption.AllDirectories);
            var orderedFiles = files.OrderBy(f => f).ToList(); // order, to be consistent

            var mainAssemblies = new List<Assembly>();

            // load plugins
            // NOTE: we load plugins in a double loop to be able to load plugins with dependencies on other plugins. Eventually, all plugins will be loaded
            var stopLoadingOuter = true;
            do
            {
                stopLoadingOuter = true;
                for (var indexOuter = orderedFiles.Count - 1; indexOuter >= 0; indexOuter--)
                {
                    var nupkgFile = orderedFiles[indexOuter];
                    try
                    {
                        var t = TryLoadNupkgFile(nupkgFile, nuGetFramework, extractedFolder);
                        if (t.HasValue)
                        {
                            var (sc, pr, assembly) = t.Value;
                            builder.Populate(sc);

                            Console.WriteLine($"Loaded OKPlugin {pr.Name}, Version {pr.Version.ToString(3)}"); // TODO: better logging

                            mainAssemblies.Add(assembly);
                        }
                        else
                        {
                            Console.WriteLine($"Encountered nupkg file ({nupkgFile}) that does not seem to be a proper OKPlugin, skipping"); // TODO: better logging
                        }

                        orderedFiles.RemoveAt(indexOuter);
                        stopLoadingOuter = false; // whenever we were able to load another plugin, we repeat the outer loop and try to load more
                    } catch (Exception e)
                    {
                        Console.WriteLine($"Could not load OKPlugin {nupkgFile}: {e.Message}"); // TODO: better error handling
                        continue;
                    }
                }
            } while (!orderedFiles.IsEmpty() && !stopLoadingOuter);

            return mainAssemblies;
        }

        private static (ServiceCollection sc, IPluginRegistration pr, Assembly assembly)? TryLoadNupkgFile(string nupkgFile, NuGetFramework nuGetFramework, string extractedFolder)
        {
            (ServiceCollection sc, IPluginRegistration pr, Assembly assembly)? ret = null;

            using var fs = File.OpenRead(nupkgFile);
            using var archive = new ZipArchive(fs);
            var relevantZipArchiveEntries = archive.Entries
                .Where(e => e.Name.EndsWith(".dll"))
                .Select(e => new
                {
                    TargetFramework = NuGetFramework.Parse(e.FullName.Split('/')[1]),
                    Entry = e
                })
                .Where(e => e.TargetFramework.Version.Major > 0 && e.TargetFramework.Version <= nuGetFramework.Version)
                .OrderBy(e => e.TargetFramework.GetShortFolderName());

            if (relevantZipArchiveEntries.Any())
            {
                var flattenedEntries = relevantZipArchiveEntries
                    .GroupBy(e => e.TargetFramework.GetShortFolderName())
                    .Last()
                    .Select(e => e.Entry)
                    .ToList();

                var pluginAssemblies = new List<string>();
                var loadContext = new PluginLoadContext();

                // load dlls
                foreach (var e in flattenedEntries)
                {
                    var finalDLLFile = Path.Combine(extractedFolder, e.Name);
                    try
                    {
                        e.ExtractToFile(finalDLLFile, overwrite: true);
                    }
                    catch (IOException)
                    {
                        // ignore, we may have already extracted this DLL
                    }
                    loadContext.AddResolverFromPath(finalDLLFile);
                    var assembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(finalDLLFile)));

                    // we use a temporary container to extract the plugin registration, which then does the actual registration
                    var tmpBuilder = new ContainerBuilder();
                    tmpBuilder.RegisterAssemblyTypes(assembly).Where(t => t.IsAssignableTo<IPluginRegistration>()).AsImplementedInterfaces().SingleInstance();
                    using var tmpContainer = tmpBuilder.Build();
                    var x = tmpContainer.ComponentRegistry.Registrations;
                    if (tmpContainer.TryResolve<IPluginRegistration>(out var pr))
                    {
                        // register plugin itself and its own services
                        var serviceCollection = new ServiceCollection();
                        pr.RegisterServices(serviceCollection);
                        serviceCollection.AddSingleton(typeof(IPluginRegistration), pr);
                        ret = (serviceCollection, pr, assembly);
                    }
                    else
                    {
                        Console.WriteLine($"Encountered DLL without IPluginRegistration, skipping! Assembly: {assembly.FullName}"); // TODO: better logging
                    }
                }
            }

            return ret;
        }

        public static void RegisterServices(ContainerBuilder builder)
        {
            builder.RegisterType<CIMappingService>().SingleInstance();
            builder.RegisterType<ManagementAuthorizationService>().As<IManagementAuthorizationService>().SingleInstance();
            builder.RegisterType<LayerBasedAuthorizationService>().As<ILayerBasedAuthorizationService>().SingleInstance();
            builder.RegisterType<DataPartitionService>().As<IDataPartitionService>().SingleInstance();
            builder.RegisterType<IngestDataService>().SingleInstance();

            builder.RegisterType<AuthRolePermissionChecker>().As<IAuthRolePermissionChecker>().SingleInstance();
            builder.RegisterType<CurrentUserAccessor>().As<ICurrentUserAccessor>().SingleInstance(); // TODO: remove, use ScopedLifetimeAccessor directly?
            builder.RegisterType<CurrentAuthorizedHttpUserService>().As<ICurrentUserService>().InstancePerLifetimeScope();

            builder.RegisterType<ScopedLifetimeAccessor>().SingleInstance();

            builder.RegisterType<DiffingCIService>().SingleInstance();

            builder.RegisterType<ArchiveOutdatedIssuesService>().As<IArchiveOutdatedIssuesService>().SingleInstance();
            builder.RegisterType<ArchiveOutdatedChangesetDataService>().As<IArchiveOutdatedChangesetDataService>().SingleInstance();
            builder.RegisterType<CalculateUnprocessedChangesetsService>().As<ICalculateUnprocessedChangesetsService>().SingleInstance();

            // authz
            builder.RegisterType<AuthzFilterManager>().As<IAuthzFilterManager>().SingleInstance();
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).AssignableTo<IAuthzFilterForMutation>().As<IAuthzFilterForMutation>().SingleInstance();
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).AssignableTo<IAuthzFilterForQuery>().As<IAuthzFilterForQuery>().SingleInstance();

            builder.RegisterType<ReactiveRunService>().As<IReactiveRunService>().SingleInstance();
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
            builder.RegisterType<ValidatorContextV1Model>().SingleInstance();
            builder.RegisterType<AuthRoleModel>().SingleInstance();
            builder.RegisterType<ChangesetDataModel>().SingleInstance();
            builder.RegisterType<RecursiveTraitModel>().SingleInstance();
            builder.RegisterType<GridViewContextModel>().SingleInstance();
            builder.RegisterType<InnerLayerDataModel>().SingleInstance();
            builder.RegisterType<ODataAPIContextModel>().SingleInstance();
            builder.RegisterType<UsageStatsModel>().As<IUsageStatsModel>().SingleInstance();
            builder.RegisterGeneric(typeof(GenericTraitEntityModel<,>)).SingleInstance();
            builder.RegisterGeneric(typeof(GenericTraitEntityModel<>)).SingleInstance();
            builder.RegisterGeneric(typeof(ReactiveGenericTraitEntityModel<>)).SingleInstance();

            // these aren't real models, but we keep them here because they are closely related to models
            builder.RegisterType<TraitsProvider>().As<ITraitsProvider>().SingleInstance();
            builder.RegisterType<EffectiveGeneratorProvider>().As<IEffectiveGeneratorProvider>().SingleInstance();
            builder.RegisterType<IssuePersister>().As<IIssuePersister>().SingleInstance();
            builder.RegisterType<CLBIssueContextSource>().As<IIssueContextSource>().SingleInstance();
            builder.RegisterType<ValidatorIssueContextSource>().As<IIssueContextSource>().SingleInstance();

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
            builder.RegisterType<CLBProcessingCache>().SingleInstance();
            builder.RegisterType<ValidatorJob>().InstancePerLifetimeScope();
            builder.RegisterType<ValidatorSingleJob>().InstancePerLifetimeScope();
            builder.RegisterType<ValidatorProcessingCache>().SingleInstance();
            builder.RegisterType<ArchiveOldDataJob>().InstancePerLifetimeScope();
            builder.RegisterType<ExternalIDManagerJob>().InstancePerLifetimeScope();
            builder.RegisterType<MarkedForDeletionJob>().InstancePerLifetimeScope();
            builder.RegisterType<UsageDataWriterJob>().InstancePerLifetimeScope();
            builder.RegisterType<GraphQLSchemaReloaderJob>().InstancePerLifetimeScope();
            builder.RegisterType<EdmModelReloaderJob>().InstancePerLifetimeScope();

            // validators
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).AssignableTo<IValidator>().AsImplementedInterfaces().SingleInstance();

            // TODO: remove
            builder.RegisterType<ReactiveTestCLB>().As<IReactiveCLB>().SingleInstance();
        }
    }
}
