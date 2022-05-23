using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class SingletonTraitEntityModel<T> : GenericTraitEntityModel<T> where T : TraitEntity, new()
    {
        public SingletonTraitEntityModel(IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel) : base(effectiveTraitModel, ciModel, attributeModel, relationModel)
        {
        }
        public async Task<(Guid, T)> TryToGet(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var a = await GetAllByCIID(layerSet, trans, timeThreshold);
            if (a.IsEmpty()) return default;

            var f = a
                .OrderBy(a => a.Key) // we order by GUID to stay consistent even when multiple CIs would match
                .First();
            return (f.Key, f.Value);
        }

        public async Task<(T dc, bool changed)> InsertOrUpdate(T t, string? ciName, LayerSet layerSet, string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            var foundT = await TryToGet(layerSet, changesetProxy.TimeThreshold, trans);

            var ciid = foundT != default ? foundT.Item1 : await ciModel.CreateCI(trans);

            var tuples = new (T t, Guid ciid)[] { (t, ciid) };
            var attributeFragments = Entities2Fragments(tuples);
            var (outgoingRelations, incomingRelations) = Entities2RelationTuples(tuples);
            var (et, changed) = await traitEntityModel.InsertOrUpdateFull(ciid, attributeFragments, outgoingRelations, incomingRelations, ciName, layerSet, writeLayer, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);

            var dc = GenericTraitEntityHelper.EffectiveTrait2Object<T>(et, attributeFieldInfos, relationFieldInfos);

            return (dc, changed);
        }
    }
}
