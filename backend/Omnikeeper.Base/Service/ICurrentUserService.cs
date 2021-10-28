using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface ICurrentUserService
    {
        Task<AuthenticatedUser> GetCurrentUser(IModelContext trans);
    }

    public interface ICurrentUserInDatabaseService
    {
        Task<UserInDatabase> GetCurrentUser(IModelContext trans);
    }
}
