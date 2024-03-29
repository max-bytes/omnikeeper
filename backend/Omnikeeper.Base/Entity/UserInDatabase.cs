﻿
using System;

namespace Omnikeeper.Base.Entity
{
    public enum UserType
    {
        Human, Robot, Unknown
    }

    public record class UserInDatabase
    {
        public long ID { get; private set; }
        public Guid UUID { get; private set; }
        public string Username { get; private set; }
        public string DisplayName { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }
        public UserType UserType { get; private set; }

        public UserInDatabase(long id, Guid uuid, string username, string displayName, UserType userType, DateTimeOffset timestamp)
        {
            ID = id;
            UUID = uuid;
            UserType = userType;
            Username = username;
            DisplayName = displayName;
            Timestamp = timestamp;
        }
    }
}
