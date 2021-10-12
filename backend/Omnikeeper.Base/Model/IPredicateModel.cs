//using Omnikeeper.Base.Entity;
//using Omnikeeper.Base.Entity.DataOrigin;
//using Omnikeeper.Base.Utils;
//using Omnikeeper.Base.Utils.ModelContext;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace Omnikeeper.Base.Model
//{
//    public interface IPredicateModel
//    {
//        Task<IDictionary<string, Predicate>> GetPredicates(LayerSet layerSet, IModelContext trans, TimeThreshold atTime);

//        Task<(Predicate predicate, bool changed)> InsertOrUpdate(string id, string wordingFrom, string wordingTo, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
//        Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
//    }

//}
