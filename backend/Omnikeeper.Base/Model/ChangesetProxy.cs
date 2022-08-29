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
        private IDictionary<string, Changeset> ActiveChangesets { get; set; }

        public async Task<Changeset> GetChangeset(string layerID, DataOriginV1 dataOrigin, IModelContext trans)
        {
            var key = $"{layerID}:{dataOrigin.Type}";
            if (ActiveChangesets.TryGetValue(key, out var changeset))
            {
                return changeset;
            }
            else
            {
                var newChangeset = await Model.CreateChangeset(User.ID, layerID, dataOrigin, trans, TimeThreshold);
                ActiveChangesets.Add(key, newChangeset);
                return newChangeset;
            }
        }

        public IEnumerable<Changeset> GetAllActiveChangesets() => ActiveChangesets.Values;

        public ChangesetProxy(UserInDatabase user, TimeThreshold timeThreshold, IChangesetModel model)
        {
            User = user;
            TimeThreshold = timeThreshold;
            Model = model;
            ActiveChangesets = new Dictionary<string, Changeset>();
        }
    }

}
