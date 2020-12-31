using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IUserInDatabaseModel
    {
        Task<UserInDatabase> UpsertUser(string username, string displayName, Guid uuid, UserType type, IModelContext trans);
        Task<UserInDatabase?> GetUser(long id, IModelContext trans);
    }
}
