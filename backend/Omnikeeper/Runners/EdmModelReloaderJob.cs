using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Controllers.OData;
using Quartz;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    [DisallowConcurrentExecution]
    public class EdmModelReloaderJob : IJob
    {
        private readonly ITraitsProvider traitsProvider;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly IServiceProvider sp;
        private readonly EdmModelHolder edmModelHolder;
        private readonly ILogger<EdmModelReloaderJob> logger;

        public EdmModelReloaderJob(ITraitsProvider traitsProvider, IModelContextBuilder modelContextBuilder, IServiceProvider sp,
            EdmModelHolder edmModelHolder, ILogger<EdmModelReloaderJob> logger)
        {
            this.traitsProvider = traitsProvider;
            this.modelContextBuilder = modelContextBuilder;
            this.sp = sp;
            this.edmModelHolder = edmModelHolder;
            this.logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                logger.LogTrace("Start");

                using (var trans = modelContextBuilder.BuildDeferred())
                {
                    // detect changes
                    var timeThreshold = TimeThreshold.BuildLatest();
                    var latestTraitChange = await traitsProvider.GetLatestChangeToActiveDataTraits(trans, timeThreshold);
                    if (latestTraitChange.HasValue)
                    {
                        var latestCreation = edmModelHolder.GetLatestModelCreation();

                        if (!latestCreation.HasValue || latestTraitChange.Value > latestCreation.Value)
                        { // reload
                            var activeTraits = await traitsProvider.GetActiveTraits(trans, timeThreshold);
                            edmModelHolder.ReInitModel(sp, activeTraits, logger);
                        }
                    }
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                logger.LogTrace($"Finished in {elapsedTime}");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error running edm-model-reloader job");
            }
        }
    }
}
