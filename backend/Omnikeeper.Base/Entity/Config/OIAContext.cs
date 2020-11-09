using Omnikeeper.Base.Inbound;
using System;

namespace Omnikeeper.Base.Entity
{
    public class OIAContext : IEquatable<OIAContext>
    {
        public long ID { get; private set; }
        public string Name { get; private set; }
        public IOnlineInboundAdapter.IConfig Config { get; private set; }

        public override int GetHashCode() => HashCode.Combine(Name, ID, Config);
        public override bool Equals(object obj) => Equals(obj as OIAContext);
        public bool Equals(OIAContext other) => other != null && Name.Equals(other.Name)
            && ID.Equals(other.ID) && Config.Equals(other.Config);

        public static OIAContext Build(string name, long id, IOnlineInboundAdapter.IConfig config)
        {
            return new OIAContext
            {
                Name = name,
                ID = id,
                Config = config
            };
        }
    }
}
