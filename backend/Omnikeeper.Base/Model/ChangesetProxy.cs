using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public class ChangesetProxy : IChangesetProxy
    {
        public DateTimeOffset Timestamp { get; private set; }
        public UserInDatabase User { get; private set; }
        public IChangesetModel Model { get; private set; }
        private Changeset? Changeset { get; set; }

        public async Task<Changeset> GetChangeset(IModelContext trans)
        {
            if (Changeset == null)
                Changeset = await Model.CreateChangeset(User.ID, trans, Timestamp);
            return Changeset;
        }

        public ChangesetProxy(UserInDatabase user, DateTimeOffset timestamp, IChangesetModel model)
        {
            User = user;
            Timestamp = timestamp;
            Model = model;
            Changeset = null;
        }
    }

}
