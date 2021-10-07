using OKPluginGenericJSONIngest.Extract;
using OKPluginGenericJSONIngest.Load;
using OKPluginGenericJSONIngest.Transform;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OKPluginGenericJSONIngest
{
    public interface IContextModel
    {
        Task<Context> GetContext(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        Task<(Guid, Context)> TryToGetContext(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        Task<IDictionary<string, Context>> GetContexts(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);

        Task<(Context context, bool changed)> InsertOrUpdate(string id, IExtractConfig extractConfig, ITransformConfig transformConfig, ILoadConfig loadConfig, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
        Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
    }

    public class ContextModel : IDBasedTraitDataConfigBaseModel<Context, string>, IContextModel
    {
        public ContextModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel)
            : base(Traits.ContextFlattenedTrait, effectiveTraitModel, ciModel, baseAttributeModel, baseRelationModel)
        { }

        public async Task<Context> GetContext(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            Context.ValidateContextIDThrow(id);

            return await Get(id, layerSet, timeThreshold, trans);
        }

        public async Task<(Guid, Context)> TryToGetContext(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            Context.ValidateContextIDThrow(id);

            return await TryToGet(id, layerSet, timeThreshold, trans);
        }

        protected override (Context, string) EffectiveTrait2DC(EffectiveTrait et)
        {
            var contextID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
            var extractConfig = TraitConfigDataUtils.ExtractMandatoryScalarJSONAttribute(et, "extract_config", Context.ExtractConfigSerializer);
            var transformConfig = TraitConfigDataUtils.ExtractMandatoryScalarJSONAttribute(et, "transform_config", Context.TransformConfigSerializer);
            var loadConfig = TraitConfigDataUtils.ExtractMandatoryScalarJSONAttribute(et, "load_config", Context.LoadConfigSerializer);
            return (new Context(contextID, extractConfig, transformConfig, loadConfig), contextID);
        }

        protected override IAttributeValue ID2AttributeValue(string id) => new AttributeScalarValueText(id);

        public async Task<IDictionary<string, Context>> GetContexts(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            return await GetAll(layerSet, trans, timeThreshold);
        }

        public async Task<(Context context, bool changed)> InsertOrUpdate(string id, IExtractConfig extractConfig, ITransformConfig transformConfig, ILoadConfig loadConfig, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await InsertOrUpdateAttributes(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                ("gji_context.id", new AttributeScalarValueText(id)),
                ("gji_context.extract_config", AttributeScalarValueJSON.Build(Context.ExtractConfigSerializer.SerializeToJObject(extractConfig))),
                ("gji_context.transform_config", AttributeScalarValueJSON.Build(Context.TransformConfigSerializer.SerializeToJObject(transformConfig))),
                ("gji_context.load_config", AttributeScalarValueJSON.Build(Context.LoadConfigSerializer.SerializeToJObject(loadConfig))),
                (ICIModel.NameAttribute, new AttributeScalarValueText($"Generic-JSON-Ingest-Context - {id}"))
            );
        }

        public async Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            return await TryToDelete(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                "gji_context.id",
                "gji_context.extract_config",
                "gji_context.transform_config",
                "gji_context.load_config",
                ICIModel.NameAttribute
            );
        }
    }
}
