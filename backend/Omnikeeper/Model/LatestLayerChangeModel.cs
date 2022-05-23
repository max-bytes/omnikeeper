using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class LatestLayerChangeModel : ILatestLayerChangeModel
    {
        private readonly IOnlineAccessProxy onlineAccessProxy;
        private readonly IChangesetModel changesetModel;

        public LatestLayerChangeModel(IOnlineAccessProxy onlineAccessProxy, IChangesetModel changesetModel)
        {
            this.onlineAccessProxy = onlineAccessProxy;
            this.changesetModel = changesetModel;
        }

        public async Task<Changeset?> GetLatestChangeInLayer(string layerID, IModelContext trans)
        {
            // check if this layer is an OIA layer, then we can't know the latest change
            if (await onlineAccessProxy.IsOnlineInboundLayer(layerID, trans))
            {
                return null;
            }
            else
            {
                var latestChangeset = await changesetModel.GetLatestChangesetForLayer(layerID, trans);
                if (latestChangeset == null)
                    return null;
                return latestChangeset;
            }
        }
    }
}
