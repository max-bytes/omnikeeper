using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public class ChangesetProxy : IChangesetProxy
    {
        public TimeThreshold TimeThreshold { get; private set; }
        public UserInDatabase User { get; private set; }
        public IChangesetModel Model { get; private set; }
        private Changeset? Changeset { get; set; }

        public async Task<Changeset> GetChangeset(IModelContext trans)
        {
            if (Changeset == null)
                Changeset = await Model.CreateChangeset(User.ID, trans, TimeThreshold.Time);
            return Changeset;
        }

        public ChangesetProxy(UserInDatabase user, TimeThreshold timeThreshold, IChangesetModel model)
        {
            User = user;
            TimeThreshold = timeThreshold;
            Model = model;
            Changeset = null;
        }
    }

}
