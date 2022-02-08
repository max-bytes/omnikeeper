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
        public QuartzJobStarter(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
                using var scope = _serviceScopeFactory.CreateScope();
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
                var scheduler = scope.ServiceProvider.GetRequiredService<IScheduler>();

                // schedule internal recurring jobs
                await ScheduleJob<CLBJob>(scheduler, "CLB", config.CLBRunnerInterval, logger);
                await ScheduleJob<MarkedForDeletionJob>(scheduler, "MarkedForDeletion", config.MarkedForDeletionRunnerInterval, logger);
                await ScheduleJob<ExternalIDManagerJob>(scheduler, "ExternalIDManager", config.ExternalIDManagerRunnerInterval, logger);
                await ScheduleJob<ArchiveOldDataJob>(scheduler, "ArchiveOldData", config.ArchiveOldDataRunnerInterval, logger);
                await ScheduleJob<UsageDataWriterJob>(scheduler, "UsageDataWriter", "0 * * * * ?", logger);

                await scheduler.Start();

                // plugin jobs
                foreach (var plugin in plugins)
                {
                    plugin.RegisterQuartzJobs();
                }
            } catch (Exception e)
            {
                logger.LogError("Error setting up Quartz scheduler: ", e);
            }
        }

        private async Task ScheduleJob<J>(IScheduler scheduler, string name, string cronSchedule, ILogger logger) where J : IJob
        {
            IJobDetail job = JobBuilder.Create<J>().WithIdentity(name, "omnikeeper").Build();

            // delete existing job, if exists
            if (await scheduler.CheckExists(job.Key))
            {
                await scheduler.DeleteJob(job.Key);
            }

            try
            {
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity($"trigger_{name}", "omnikeeper")
                    .StartNow()
                    .WithCronSchedule(cronSchedule)
                    .Build();
                await scheduler.ScheduleJob(job, trigger);
            }
            catch (Exception e)
            {
                logger.LogError($"Error scheduling job {name}, skipping", e);
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
