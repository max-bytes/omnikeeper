using Landscape.Base.Entity;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public class ChangesetProxy : IChangesetProxy
    {
        public DateTimeOffset Timestamp { get; private set; }
        public UserInDatabase User { get; private set; }
        public IChangesetModel Model { get; private set; }
        private Changeset Changeset { get; set; }

        public async Task<Changeset> GetChangeset(NpgsqlTransaction trans)
        {
            if (Changeset == null)
                Changeset = await Model.CreateChangeset(User.ID, trans, Timestamp);
            return Changeset;
        }

        public static ChangesetProxy Build(UserInDatabase user, DateTimeOffset timestamp, IChangesetModel model)
        {
            var c = new ChangesetProxy
            {
                User = user,
                Timestamp = timestamp,
                Model = model,
                Changeset = null
            };
            return c;
        }
    }

}
