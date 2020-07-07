using Landscape.Base.Inbound;
using System;
using System.Drawing;

namespace Landscape.Base.Entity
{
    public class OIAConfig : IEquatable<OIAConfig>
    {
        public long ID { get; private set; }
        public string Name { get; private set; }
        public IOnlineInboundAdapter.IConfig Config { get; private set; }

        public override int GetHashCode() => HashCode.Combine(Name, ID, Config);
        public override bool Equals(object obj) => Equals(obj as OIAConfig);
        public bool Equals(OIAConfig other) => other != null && Name.Equals(other.Name)
            && ID.Equals(other.ID) && Config.Equals(other.Config);

        public static OIAConfig Build(string name, long id, IOnlineInboundAdapter.IConfig config)
        {
            return new OIAConfig
            {
                Name = name,
                ID = id,
                Config = config
            };
        }
    }
}
