using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GridView.Entity;
using Omnikeeper.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Model
{
    public class GridViewContextModel : TraitDataConfigBaseModel<FullContext>, IGridViewContextModel
    {
        public GridViewContextModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel)
            :base(CoreTraits.GridviewContextFlattened, effectiveTraitModel, ciModel, baseAttributeModel)
        {
        }

        public async Task<IDictionary<string, FullContext>> GetFullContexts(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            return await GetAll(layerSet, trans, timeThreshold);
        }

        public async Task<FullContext> GetFullContext(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidateGridViewContextIDThrow(id);

            return await Get(id, layerSet, timeThreshold, trans);
        }

        public async Task<(Guid, FullContext)> TryToGetFullContext(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidateGridViewContextIDThrow(id);

            return await TryToGet(id, layerSet, timeThreshold, trans);
        }

        protected override (FullContext dc, string id) EffectiveTrait2DC(EffectiveTrait et)
        {
            var contextID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(et, "id");
            var speakingName = TraitConfigDataUtils.ExtractOptionalScalarTextAttribute(et, "speaking_name");
            var description = TraitConfigDataUtils.ExtractOptionalScalarTextAttribute(et, "description");
            var config = TraitConfigDataUtils.ExtractMandatoryScalarJSONAttribute(et, "config", GridViewConfiguration.Serializer);

            return (new FullContext(contextID, speakingName, description, config), contextID);
        }

        public async Task<(FullContext fullContext, bool changed)> InsertOrUpdate(string id, string speakingName, string description, GridViewConfiguration configuration, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            IDValidations.ValidateGridViewContextIDThrow(id);

            return await InsertOrUpdate(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                ("gridview_context.id", new AttributeScalarValueText(id)),
                ("gridview_context.config", AttributeScalarValueJSON.Build(GridViewConfiguration.Serializer.SerializeToJObject(configuration))),
                ("gridview_context.speaking_name", new AttributeScalarValueText(speakingName)),
                ("gridview_context.description", new AttributeScalarValueText(description)),
                (ICIModel.NameAttribute, new AttributeScalarValueText($"Gridview-Context - {id}"))
            );
        }

        public async Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            IDValidations.ValidateGridViewContextIDThrow(id);

            return await TryToDelete(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans,
                "gridview_context.id",
                "gridview_context.config",
                "gridview_context.speaking_name",
                "gridview_context.description",
                ICIModel.NameAttribute
            );
        }
    }
}
