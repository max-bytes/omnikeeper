using Omnikeeper.Base.Utils;
using System;

namespace Omnikeeper.Base.Entity
{
    // TODO: make ID private and move away from strings as layer-IDs but instead only use this class
    // TODO: rename Layer to LayerID
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
            IDValidations.ValidateLayerIDThrow(id);

            return new Layer(id);
        }
    }
}
