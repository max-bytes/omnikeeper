using Autofac.Features.Indexed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.Startup
{
    // taken from https://github.com/quartznet/quartznet/blob/main/src/Quartz.AspNetCore/AspNetCore/HealthChecks/QuartzHealthCheck.cs
    public abstract class QuartzHealthCheck : IHealthCheck
    {
        private readonly ISchedulerFactory schedulerFactory;
        private readonly string schedulerName;

        public QuartzHealthCheck(IIndex<string, ISchedulerFactory> schedulersFactories, string schedulerName)
        {
            schedulerFactory = schedulersFactories[schedulerName];
            this.schedulerName = schedulerName;
        }

        async Task<HealthCheckResult> IHealthCheck.CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            var localScheduler = await schedulerFactory.GetScheduler(cancellationToken);
            if (!localScheduler.IsStarted)
            {
                return HealthCheckResult.Unhealthy($"Quartz scheduler \"{schedulerName}\" is not running");
            }

            try
            {
                // Ask for a job we know doesn't exist
                await localScheduler.CheckExists(new JobKey(Guid.NewGuid().ToString()), cancellationToken);
            }
            catch (SchedulerException)
            {
                return HealthCheckResult.Unhealthy($"Quartz scheduler \"{schedulerName}\" cannot connect to the store");
            }

            return HealthCheckResult.Healthy($"Quartz scheduler \"{schedulerName}\" is ready");
        }
    }

    public sealed class LocalQuartzHealthCheck : QuartzHealthCheck
    {
        public LocalQuartzHealthCheck(IIndex<string, ISchedulerFactory> schedulersFactories) : base(schedulersFactories, "local") { }
    }
    public sealed class DistributedQuartzHealthCheck : QuartzHealthCheck
    {
        public DistributedQuartzHealthCheck(IIndex<string, ISchedulerFactory> schedulersFactories) : base(schedulersFactories, "distributed") { }
    }
}
