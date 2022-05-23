using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ILatestLayerChangeModel
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="layerID"></param>
        /// <param name="trans"></param>
        /// <returns>returns the datetime of the latest change that happened in/to this layer; returns null if there has not been any change yet or the layer is an online layer</returns>
        Task<Changeset?> GetLatestChangeInLayer(string layerID, IModelContext trans); // TODO: remove, not needed
    }
}
