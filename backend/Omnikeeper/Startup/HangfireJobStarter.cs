using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Runners;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.Startup
{
    public class HangfireJobStarter : BackgroundService, IHostedService
    {
        public IServiceScopeFactory _serviceScopeFactory;
        public HangfireJobStarter(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var metaConfigurationModel = scope.ServiceProvider.GetRequiredService<IMetaConfigurationModel>();
            var baseConfigurationModel = scope.ServiceProvider.GetRequiredService<IBaseConfigurationModel>();
            var modelContextBuilder = scope.ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var trans = modelContextBuilder.BuildImmediate();
            var metaConfig = await metaConfigurationModel.GetConfigOrDefault(trans);
            var config = await baseConfigurationModel.GetConfigOrDefault(new LayerSet(metaConfig.ConfigLayerset), TimeThreshold.BuildLatest(), trans);

            var plugins = scope.ServiceProvider.GetServices<IPluginRegistration>();

            RecurringJob.RemoveIfExists("CLBRunner");
            RecurringJob.RemoveIfExists("MarkedForDeletionRunner");
            RecurringJob.RemoveIfExists("ExternalIDManagerRunner");
            RecurringJob.RemoveIfExists("ArchiveOldDataRunner");
            RecurringJob.RemoveIfExists("UsageDataWriteRunner");

            //RecurringJob.AddOrUpdate<CLBRunner>("CLBRunner", s => s.Run(null), config.CLBRunnerInterval);
            //RecurringJob.AddOrUpdate<MarkedForDeletionRunner>("MarkedForDeletionRunner", s => s.Run(null), config.MarkedForDeletionRunnerInterval);
            //RecurringJob.AddOrUpdate<ExternalIDManagerRunner>("ExternalIDManagerRunner", s => s.Run(null), config.ExternalIDManagerRunnerInterval);
            //RecurringJob.AddOrUpdate<ArchiveOldDataRunner>("ArchiveOldDataRunner", s => s.Run(null), config.ArchiveOldDataRunnerInterval);
            //RecurringJob.AddOrUpdate<UsageDataWriteRunner>("UsageDataWriteRunner", s => s.Run(null), Cron.Minutely);

            // plugin hangfire jobs
            foreach (var plugin in plugins)
            {
                plugin.RegisterHangfireJobRunners();
            }
        }
    }
}
