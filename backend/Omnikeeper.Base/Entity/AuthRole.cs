using System;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    [TraitEntity("__meta.config.auth_role", TraitOriginType.Core)]
    public class AuthRole : TraitEntity, IEquatable<AuthRole>
    {
        public AuthRole() { ID = ""; Permissions = new string[0]; Name = ""; }

        public AuthRole(string iD, string[] permissions)
        {
            ID = iD;
            Permissions = permissions;
            Name = $"Auth-Role - {ID}";
        }

        [TraitAttribute("id", "auth_role.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("permissions", "auth_role.permissions", optional: true)]
        public string[] Permissions;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public string Name;

        public override bool Equals(object? obj) => Equals(obj as AuthRole);
        public bool Equals(AuthRole? other)
        {
            return other != null && ID == other.ID && Name == other.Name &&
                   Permissions.SequenceEqual(other.Permissions);
        }
        public override int GetHashCode() => HashCode.Combine(ID, Permissions, Name);
    }
}
