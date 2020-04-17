
using System;

namespace Landscape.Base.Entity
{
    public class CIType : IEquatable<CIType>
    {
        public string ID { get; private set; }
        public static CIType Build(string id)
        {
            return new CIType
            {
                ID = id
            };
        }

        public override int GetHashCode() => HashCode.Combine(ID);
        public override bool Equals(object obj) => Equals(obj as CIType);
        public bool Equals(CIType other) => other != null && ID == other.ID;

        // TODO: this method of dealing with non-specified Type for CIs sucks, find better way
        public static readonly CIType UnspecifiedCIType = Build("UNSPECIFIED");
    }
}
