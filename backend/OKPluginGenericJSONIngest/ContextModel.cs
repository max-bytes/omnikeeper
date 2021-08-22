using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OKPluginGenericJSONIngest
{
    public interface IContextModel
    {
        Task<Context> GetContext(string id, TimeThreshold timeThreshold, IModelContext trans);
        Task<(Guid, Context)> TryToGetContext(string id, TimeThreshold timeThreshold, IModelContext trans);
        Task<IDictionary<string, Context>> GetContexts(TimeThreshold timeThreshold, IModelContext trans);
    }

    public class ContextModel : TraitDataConfigBaseModel<Context>, IContextModel
    {
        public ContextModel(IBaseConfigurationModel baseConfigurationModel, IEffectiveTraitModel effectiveTraitModel)
            : base(Traits.ContextFlattenedTrait, baseConfigurationModel, effectiveTraitModel)
        { }

        public async Task<Context> GetContext(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            Context.ValidateContextIDThrow(id);

            return await Get(id, timeThreshold, trans);
        }

        public async Task<(Guid, Context)> TryToGetContext(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            Context.ValidateContextIDThrow(id);

            return await TryToGet(id, timeThreshold, trans);
        }

        protected override (Context, string) EffectiveTrait2DC(EffectiveTrait et)
        {
            var contextID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
            var extractConfig = TraitConfigDataUtils.ExtractMandatoryScalarJSONAttribute(et, "extract_config", Context.ExtractConfigSerializer);
            var transformConfig = TraitConfigDataUtils.ExtractMandatoryScalarJSONAttribute(et, "transform_config", Context.TransformConfigSerializer);
            var loadConfig = TraitConfigDataUtils.ExtractMandatoryScalarJSONAttribute(et, "load_config", Context.LoadConfigSerializer);
            return (new Context(contextID, extractConfig, transformConfig, loadConfig), contextID);
        }

        public async Task<IDictionary<string, Context>> GetContexts(TimeThreshold timeThreshold, IModelContext trans)
        {
            return await GetAll(trans, timeThreshold);
        }
    }
}
