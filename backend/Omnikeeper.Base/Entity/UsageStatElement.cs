
using System;

namespace Omnikeeper.Base.Entity
{
    public enum UsageStatsOperation
    {
        Use,
        Read,
        Write
    }
    public class UsageStatElement
    {
        public readonly string Type;
        public readonly string Name;
        public readonly string Username;
        public readonly string LayerID;
        public readonly UsageStatsOperation Operation;
        public readonly DateTimeOffset Timestamp;

        public UsageStatElement(string type, string name, string username, string layerID, UsageStatsOperation operation, DateTimeOffset timestamp)
        {
            Type = type;
            Name = name;
            Username = username;
            LayerID = layerID;
            Operation = operation;
            Timestamp = timestamp;
        }
    }
}
