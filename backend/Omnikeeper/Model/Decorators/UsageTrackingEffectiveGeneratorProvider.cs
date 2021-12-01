using Autofac;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class UsageTrackingEffectiveGeneratorProvider : IEffectiveGeneratorProvider
    {
        private readonly IEffectiveGeneratorProvider @base;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;

        public UsageTrackingEffectiveGeneratorProvider(IEffectiveGeneratorProvider @base, ScopedLifetimeAccessor scopedLifetimeAccessor)
        {
            this.@base = @base;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
        }

        private void TrackEffectiveGeneratorUsage(string generatorID)
        {
            var usageTracker = scopedLifetimeAccessor.GetLifetimeScope()?.Resolve<IScopedUsageTracker>();
            if (usageTracker != null)
                usageTracker.TrackUseGenerator(generatorID);
        }

        public async Task<IEnumerable<GeneratorV1>[]> GetEffectiveGenerators(string[] layerIDs, IGeneratorSelection generatorSelection, IAttributeSelection attributeSelection, IModelContext trans, TimeThreshold timeThreshold)
        {
            var r = await @base.GetEffectiveGenerators(layerIDs, generatorSelection, attributeSelection, trans, timeThreshold);

            foreach (var generators in r)
                foreach (var generator in generators)
                    TrackEffectiveGeneratorUsage(generator.ID);

            return r;
        }
    }
}
