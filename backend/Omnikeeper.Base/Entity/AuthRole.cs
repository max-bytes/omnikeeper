using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Entity
{
    [TraitEntity("__meta.config.auth_role", TraitOriginType.Core)]
    public class AuthRole : TraitEntity, IEquatable<AuthRole>
    {
        public AuthRole() : base(null) { ID = ""; Permissions = new string[0]; Name = ""; }

        public AuthRole(Guid? ciid, string iD, string[] permissions) : base(ciid)
        {
            ID = iD;
            Permissions = permissions;
            Name = $"Auth-Role - {ID}";
        }

        [TraitAttribute("id", "auth_role.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string ID;

        [TraitAttribute("permissions", "auth_role.permissions", optional: true)]
        public readonly string[] Permissions;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Name;

        public override bool Equals(object? obj) => Equals(obj as AuthRole);
        public bool Equals(AuthRole? other)
        {
            return other != null && ID == other.ID && Name == other.Name &&
                   Permissions.SequenceEqual(other.Permissions);
        }
        public override int GetHashCode() => HashCode.Combine(ID, Permissions, Name);
    }
}
