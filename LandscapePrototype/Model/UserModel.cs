﻿using Landscape.Base.Model;
using LandscapePrototype.Entity;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LandscapePrototype.Model.RelationModel;

namespace LandscapePrototype.Model
{
    public class UserModel : IUserModel
    {
        private readonly NpgsqlConnection conn;

        public UserModel(NpgsqlConnection connection)
        {
            conn = connection;
        }

        public async Task<User> CreateOrUpdateFetchUser(string username, Guid uuid, NpgsqlTransaction trans)
        {
            // check for an updated user first
            var existingUser = await GetUser(username, uuid, trans);
            if (existingUser != null)
                return existingUser;

            using var command = new NpgsqlCommand(@"INSERT INTO ""user"" (keycloak_id, timestamp, username) VALUES (@uuid, now(), @username) returning id, timestamp", conn, trans);
            command.Parameters.AddWithValue("uuid", uuid);
            command.Parameters.AddWithValue("username", username);
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var id = reader.GetInt64(0);
            var timestamp = reader.GetDateTime(1);
            return User.Build(id, uuid, username, timestamp);
        }

        public async Task<User> GetUser(long id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"SELECT keycloak_id, username, timestamp FROM ""user"" WHERE id = @id LIMIT 1", conn, trans);

            command.Parameters.AddWithValue("id", id);
            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return null;

            var uuid = dr.GetGuid(0);
            var username = dr.GetString(1);
            var timestamp = dr.GetTimeStamp(2).ToDateTime();
            return User.Build(id, uuid, username, timestamp);
        }

        private async Task<User> GetUser(string username, Guid uuid, NpgsqlTransaction trans)
        {
            // TODO: order by timestamp and get latest user
            using var command = new NpgsqlCommand(@"SELECT id, timestamp FROM ""user"" WHERE keycloak_id = @uuid AND username = @username", conn, trans);

            command.Parameters.AddWithValue("uuid", uuid);
            command.Parameters.AddWithValue("username", username);
            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return null;

            var id = dr.GetInt64(0);
            var timestamp = dr.GetTimeStamp(1).ToDateTime();
            return User.Build(id, uuid, username, timestamp);
        }
    }
}
