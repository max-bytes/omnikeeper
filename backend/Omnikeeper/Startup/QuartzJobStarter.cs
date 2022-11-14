using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Runners;
using Quartz;
using Quartz.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.Startup
{
    public class QuartzJobStarter : BackgroundService, IHostedService
    {
        public IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration configuration;
        private static readonly JobKey JKCLB = new("CLB", "omnikeeper");
        private static readonly JobKey JKValidator = new("Validator", "omnikeeper");
        private static readonly JobKey JKMarkedForDeletion = new("MarkedForDeletion", "omnikeeper");
        private static readonly JobKey JKExternalIDManager = new("ExternalIDManager", "omnikeeper");
        private static readonly JobKey JKArchiveOldData = new("ArchiveOldData", "omnikeeper");
        private static readonly JobKey JKUsageDataWriter = new("UsageDataWriter", "omnikeeper");
        public static readonly JobKey JKTraitsReloader = new("TraitsReloader", "omnikeeper");
        public static readonly JobKey JKGraphQLSchemaReloader = new("GraphQLSchemaReloader", "omnikeeper");
        public static readonly JobKey JKEdmModelReloader = new("EdmModelReloader", "omnikeeper");

        public QuartzJobStarter(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
        {
            _serviceScopeFactory = serviceScopeFactory;
            this.configuration = configuration;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var lifetimeScope = scope.ServiceProvider.GetRequiredService<ILifetimeScope>();
            var metaConfigurationModel = scope.ServiceProvider.GetRequiredService<IMetaConfigurationModel>();
            var baseConfigurationModel = scope.ServiceProvider.GetRequiredService<IBaseConfigurationModel>();
            var modelContextBuilder = scope.ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<QuartzJobStarter>>();
            var trans = modelContextBuilder.BuildImmediate();
            var metaConfig = await metaConfigurationModel.GetConfigOrDefault(trans);
            var config = await baseConfigurationModel.GetConfigOrDefault(new LayerSet(metaConfig.ConfigLayerset), TimeThreshold.BuildLatest(), trans);

            var plugins = scope.ServiceProvider.GetServices<IPluginRegistration>();

            try
            {
                var lf = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                LogProvider.SetCurrentLogProvider(new QuartzLogProvider(lf));
                var localScheduler = lifetimeScope.ResolveKeyed<IScheduler>("localScheduler");
                var distributedScheduler = lifetimeScope.ResolveKeyed<IScheduler>("distributedScheduler");

                bool deleteOnly = false; // TODO: only set to true for debugging purposes

                // schedule internal recurring jobs
                await ScheduleJob<MarkedForDeletionJob>(distributedScheduler, JKMarkedForDeletion, config.MarkedForDeletionRunnerInterval, logger, deleteOnly, 0);
                await ScheduleJob<ExternalIDManagerJob>(distributedScheduler, JKExternalIDManager, config.ExternalIDManagerRunnerInterval, logger, deleteOnly, 0);
                await ScheduleJob<ArchiveOldDataJob>(distributedScheduler, JKArchiveOldData, config.ArchiveOldDataRunnerInterval, logger, deleteOnly, 0);

                if (configuration.GetValue("RunComputeLayers", false))
                {
                    await ScheduleJob<CLBJob>(localScheduler, JKCLB, config.CLBRunnerInterval, logger, deleteOnly, 10);
                    await ScheduleJob<ValidatorJob>(localScheduler, JKValidator, config.CLBRunnerInterval, logger, deleteOnly, -10); // TODO: add own settings
                }
                await ScheduleJob<UsageDataWriterJob>(localScheduler, JKUsageDataWriter, "0 * * * * ?", logger, deleteOnly, -20);
                await ScheduleJob<TraitsReloaderJob>(localScheduler, JKTraitsReloader, "*/5 * * * * ?", logger, deleteOnly, 20);
                await ScheduleJob<GraphQLSchemaReloaderJob>(localScheduler, JKGraphQLSchemaReloader, "*/5 * * * * ?", logger, deleteOnly, 20);
                await ScheduleJob<EdmModelReloaderJob>(localScheduler, JKEdmModelReloader, "*/5 * * * * ?", logger, deleteOnly, 20);

                await distributedScheduler.Start(stoppingToken);
                await localScheduler.Start(stoppingToken);

                // plugin jobs
                foreach (var plugin in plugins)
                {
                    plugin.RegisterQuartzJobs();
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error setting up Quartz scheduler");
            }
        }

        private static async Task ScheduleJob<J>(IScheduler scheduler, JobKey jobKey, string cronSchedule, ILogger logger, bool deleteOnly, int priority) where J : IJob
        {
            IJobDetail job = JobBuilder.Create<J>().WithIdentity(jobKey).Build();

            // delete existing job, if exists
            if (await scheduler.CheckExists(job.Key))
            {
                await scheduler.DeleteJob(job.Key);
            }

            if (deleteOnly) return;

            try
            {
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity($"trigger_for_job_{jobKey.Name}", jobKey.Group)
                    .WithPriority(priority)
                    .StartNow()
                    .WithCronSchedule(cronSchedule)
                    .Build();
                await scheduler.ScheduleJob(job, trigger);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error scheduling job {jobKey.Name}, skipping");
            }
        }
    }

    // TODO: better integrate into rest of logging
    class QuartzLogProvider : ILogProvider
    {
        private readonly ILoggerFactory loggerFactory;

        public QuartzLogProvider(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }
        public Logger GetLogger(string name)
        {
            return (level, func, exception, parameters) =>
            {
                if (func != null)
                {
                    var logger = loggerFactory.CreateLogger(name);

                    var translatedLevel = level switch
                    {
                        Quartz.Logging.LogLevel.Trace => Microsoft.Extensions.Logging.LogLevel.Trace,
                        Quartz.Logging.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
                        Quartz.Logging.LogLevel.Info => Microsoft.Extensions.Logging.LogLevel.Information,
                        Quartz.Logging.LogLevel.Warn => Microsoft.Extensions.Logging.LogLevel.Warning,
                        Quartz.Logging.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
                        Quartz.Logging.LogLevel.Fatal => Microsoft.Extensions.Logging.LogLevel.Critical,
                        _ => throw new NotImplementedException(),
                    };

                    logger.Log(translatedLevel, exception, func(), parameters);
                }
                return true;
            };
        }

        public IDisposable OpenNestedContext(string message)
        {
            throw new NotImplementedException();
        }

        public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
        {
            throw new NotImplementedException();
        }
    }

}
