using ProtoBuf;
using System;

namespace Omnikeeper.Base.Entity
{
    [ProtoContract(SkipConstructor = true)]
    public class AuthRole : IEquatable<AuthRole>
    {
        public AuthRole(string iD, string[] permissions)
        {
            ID = iD;
            Permissions = permissions;
        }

        [ProtoMember(1)] public readonly string ID;
        [ProtoMember(2)] public readonly string[] Permissions;

        public override bool Equals(object? obj) => Equals(obj as AuthRole);
        public bool Equals(AuthRole? other)
        {
            return other != null && ID == other.ID &&
                   Permissions.Equals(other.Permissions);
        }
        public override int GetHashCode() => HashCode.Combine(ID, Permissions);
    }
}
