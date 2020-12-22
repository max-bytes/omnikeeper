using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class UserInDatabaseModel : IUserInDatabaseModel
    {
        public async Task<UserInDatabase> UpsertUser(string username, string displayName, Guid uuid, UserType type, IModelContext trans)
        {
            // check for an updated user first
            var existingUser = await GetUser(username, uuid, trans);
            if (existingUser != null && existingUser.UserType == type && existingUser.DisplayName == displayName)
                return existingUser;

            using var command = new NpgsqlCommand(@"INSERT INTO ""user"" (keycloak_id, timestamp, type, username, displayName) VALUES (@uuid, @timestamp, @type, @username, @displayName) returning id, timestamp", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("uuid", uuid);
            command.Parameters.AddWithValue("timestamp", DateTimeOffset.Now);
            command.Parameters.AddWithValue("username", username);
            command.Parameters.AddWithValue("displayName", displayName);
            command.Parameters.AddWithValue("type", type);
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var id = reader.GetInt64(0);
            var timestamp = reader.GetDateTime(1);
            return new UserInDatabase(id, uuid, username, displayName, type, timestamp);
        }

        public async Task<UserInDatabase?> GetUser(long id, IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"SELECT keycloak_id, username, displayName, type, timestamp FROM ""user"" WHERE id = @id ORDER BY timestamp DESC LIMIT 1", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("id", id);
            command.Prepare();
            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return null;

            var uuid = dr.GetGuid(0);
            var username = dr.GetString(1);
            var displayName = dr.GetString(2);
            var usertype = dr.GetFieldValue<UserType>(3);
            var timestamp = dr.GetTimeStamp(4).ToDateTime();
            return new UserInDatabase(id, uuid, username, displayName, usertype, timestamp);
        }

        private async Task<UserInDatabase?> GetUser(string username, Guid uuid, IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"SELECT id, timestamp, type, displayName FROM ""user"" WHERE keycloak_id = @uuid AND username = @username ORDER BY timestamp DESC LIMIT 1", trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("uuid", uuid);
            command.Parameters.AddWithValue("username", username);
            command.Prepare();
            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return null;

            var id = dr.GetInt64(0);
            var timestamp = dr.GetTimeStamp(1).ToDateTime();
            var usertype = dr.GetFieldValue<UserType>(2);
            var displayName = dr.GetString(3);
            return new UserInDatabase(id, uuid, username, displayName, usertype, timestamp);
        }
    }
}
