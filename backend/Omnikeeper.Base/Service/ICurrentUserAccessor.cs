using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface ICurrentUserAccessor
    {
        Task<AuthenticatedUser> GetCurrentUser(IModelContext trans);
        string GetCurrentUsername();
    }

    // NOTE: must NOT be passed as constructor parameter to singleton instances
    public interface ICurrentUserService
    {
        Task<AuthenticatedUser> GetCurrentUser(IModelContext trans);
        string GetCurrentUsername();
    }
}
