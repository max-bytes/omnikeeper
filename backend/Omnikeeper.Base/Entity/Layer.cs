using System;

namespace Omnikeeper.Base.Entity
{
    public class Layer : IEquatable<Layer>
    {
        private Layer(string id)
        {
            ID = id;
        }

        public readonly string ID;

        public override int GetHashCode() => HashCode.Combine(ID);
        public override bool Equals(object? obj) => Equals(obj as Layer);
        public bool Equals(Layer? other) => other != null && ID == other.ID;

        public static Layer Build(string id)
        {
            return new Layer(id);
        }
    }
}
