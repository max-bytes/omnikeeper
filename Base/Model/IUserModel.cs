using LandscapePrototype.Entity;
using LandscapePrototype.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IUserModel
    {
        Task<User> CreateOrUpdateFetchUser(string username, Guid uuid, NpgsqlTransaction trans);
    }
}
