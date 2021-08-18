using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Entity;
using Omnikeeper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Model
{
    public class GridViewContextModel : IGridViewContextModel
    {
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;

        public GridViewContextModel(IBaseConfigurationModel baseConfigurationModel, IEffectiveTraitModel effectiveTraitModel)
        {
            this.baseConfigurationModel = baseConfigurationModel;
            this.effectiveTraitModel = effectiveTraitModel;
        }

        public async Task<IDictionary<string, Context>> GetContexts(TimeThreshold timeThreshold, IModelContext trans)
        {
            // derive config layerset from base config
            var baseConfig = await baseConfigurationModel.GetConfigOrDefault(trans);
            var configLayerset = new LayerSet(baseConfig.ConfigLayerset);

            var contextCIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(CoreTraits.GridviewContextFlattened, configLayerset, new AllCIIDsSelection(), trans, timeThreshold);
            var ret = new Dictionary<string, Context>();
            foreach (var (_, contextET) in contextCIs.Values)
            {
                var p = EffectiveTrait2Context(contextET);
                ret.Add(p.ID, p);
            }
            return ret;
        }

        private Context EffectiveTrait2Context(EffectiveTrait et)
        {
            var contextID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
            var speakingName = TraitConfigDataUtils.ExtractOptionalScalarTextAttribute(et, "speaking_name");
            var description = TraitConfigDataUtils.ExtractOptionalScalarTextAttribute(et, "description");
            return new Context(contextID, speakingName, description);
        }

        private FullContext EffectiveTrait2FullContext(EffectiveTrait et)
        {
            var context = EffectiveTrait2Context(et);
            var config = TraitConfigDataUtils.DeserializeMandatoryScalarJSONAttribute(et, "config", GridViewConfiguration.Serializer);

            return new FullContext(context.ID, context.SpeakingName, context.Description, config);
        }

        public async Task<FullContext> GetFullContext(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            var t = await TryToGetFullContext(id, timeThreshold, trans);
            if (t.Equals(default))
            {
                throw new Exception($"Could not find context with ID {id}");
            }
            else
            {
                return t.Item2;
            }
        }

        public async Task<(Guid, FullContext)> TryToGetFullContext(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            // derive config layerset from base config
            var baseConfig = await baseConfigurationModel.GetConfigOrDefault(trans);
            var configLayerset = new LayerSet(baseConfig.ConfigLayerset);

            // TODO: better performance possible?
            var contextCIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(CoreTraits.GridviewContextFlattened, configLayerset, new AllCIIDsSelection(), trans, timeThreshold);

            var foundContextCIs = contextCIs.Where(pci => pci.Value.et.TraitAttributes["id"].Attribute.Value.Value2String() == id)
                .OrderBy(t => t.Key); // we order by GUID to stay consistent even when multiple CIs would match

            var foundContextCI = foundContextCIs.FirstOrDefault();
            if (!foundContextCI.Equals(default(KeyValuePair<Guid, (MergedCI ci, EffectiveTrait et)>)))
            {
                return (foundContextCI.Key, EffectiveTrait2FullContext(foundContextCI.Value.et));
            }
            return default;
        }
    }
}
