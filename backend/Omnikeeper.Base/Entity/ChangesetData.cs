using System;

namespace Omnikeeper.Base.Entity
{
    [TraitEntity("__meta.changeset.changeset_data", TraitOriginType.Core)]
    public class ChangesetData : TraitEntity, IEquatable<ChangesetData>
    {
        public ChangesetData() { ID = ""; Name = ""; }

        public ChangesetData(string id)
        {
            ID = id;
            Name = $"Changeset-Data - {id}";
        }

        [TraitAttribute("id", "changeset_data.changeset_id")]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public string Name;

        public override bool Equals(object? obj) => Equals(obj as ChangesetData);
        public bool Equals(ChangesetData? other)
        {
            return other != null && ID == other.ID && Name == other.Name;
        }
        public override int GetHashCode() => HashCode.Combine(ID, Name);
    }
}
