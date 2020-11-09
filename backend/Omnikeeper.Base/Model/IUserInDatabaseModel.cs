﻿using Npgsql;
using Omnikeeper.Base.Entity;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IUserInDatabaseModel
    {
        Task<UserInDatabase> UpsertUser(string username, string displayName, Guid uuid, UserType type, NpgsqlTransaction trans);
        Task<UserInDatabase> GetUser(long id, NpgsqlTransaction trans);
    }
}
