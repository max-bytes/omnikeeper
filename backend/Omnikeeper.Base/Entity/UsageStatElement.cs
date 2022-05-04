
using System;

namespace Omnikeeper.Base.Entity
{
    public class UsageStatElement
    {
        public readonly string Type;
        public readonly string Name;
        public readonly string Username;
        public readonly string LayerID;
        public readonly DateTimeOffset timestamp;

        public UsageStatElement(string type, string name, string username, string layerID, DateTimeOffset timestamp)
        {
            Type = type;
            Name = name;
            Username = username;
            LayerID = layerID;
            this.timestamp = timestamp;
        }
    }
}
