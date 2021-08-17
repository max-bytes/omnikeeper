using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
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

        // TODO: check if we can move DataOrigin into changeset as well, and even remove them from attributes/relations
        public async Task<Changeset> GetChangeset(string layerID, IModelContext trans)
        {
            if (ActiveChangesets.TryGetValue(layerID, out var changeset))
            {
                return changeset;
            } else
            {
                var newChangeset = await Model.CreateChangeset(User.ID, layerID, trans, TimeThreshold.Time);
                ActiveChangesets.Add(layerID, newChangeset);
                return newChangeset;
            }
        }

        public ChangesetProxy(UserInDatabase user, TimeThreshold timeThreshold, IChangesetModel model)
        {
            User = user;
            TimeThreshold = timeThreshold;
            Model = model;
            ActiveChangesets = new Dictionary<string, Changeset>();
        }
    }

}
