using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Omnikeeper.Base.Model.Config;
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
            var baseConfigurationModel = scope.ServiceProvider.GetRequiredService<IBaseConfigurationModel>();

            var config = await baseConfigurationModel.GetConfigOrDefault(null);

            RecurringJob.AddOrUpdate<CLBRunner>(s => s.Run(null), config.CLBRunnerInterval);
            RecurringJob.AddOrUpdate<MarkedForDeletionRunner>(s => s.Run(null), config.MarkedForDeletionRunnerInterval);
            RecurringJob.AddOrUpdate<ExternalIDManagerRunner>(s => s.Run(null), config.ExternalIDManagerRunnerInterval);
            RecurringJob.AddOrUpdate<ArchiveOldDataRunner>(s => s.Run(null), config.ArchiveOldDataRunnerInterval);
        }
    }
}
