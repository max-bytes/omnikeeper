using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public class ChangesetProxy : IChangesetProxy
    {
        public TimeThreshold TimeThreshold { get; private set; }
        public UserInDatabase User { get; private set; }
        public IChangesetModel Model { get; private set; }
        public DataOriginV1 DataOrigin { get; private set; }
        private IDictionary<string, Changeset> ActiveChangesets { get; set; }

        public async Task<Changeset> GetChangeset(string layerID, IModelContext trans)
        {
            if (ActiveChangesets.TryGetValue(layerID, out var changeset))
            {
                return changeset;
            }
            else
            {
                var newChangeset = await Model.CreateChangeset(User.ID, layerID, DataOrigin, trans, TimeThreshold);
                ActiveChangesets.Add(layerID, newChangeset);
                return newChangeset;
            }
        }

        public IEnumerable<Changeset> GetAllActiveChangesets() => ActiveChangesets.Values;
        public Changeset? GetActiveChangeset(string layerID)
        {
            if (ActiveChangesets.TryGetValue(layerID, out var c))
                return c;
            return null;
        }

        public ChangesetProxy(UserInDatabase user, TimeThreshold timeThreshold, IChangesetModel model, DataOriginV1 dataOrigin)
        {
            User = user;
            TimeThreshold = timeThreshold;
            Model = model;
            DataOrigin = dataOrigin;
            ActiveChangesets = new Dictionary<string, Changeset>();
        }
    }

}
