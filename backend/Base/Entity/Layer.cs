using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Landscape.Base.Entity
{
    public class Layer : IEquatable<Layer>
    {
        public string Name { get; private set; }
        public long ID { get; private set; }
        public AnchorState State { get; private set; }

        public override int GetHashCode() => HashCode.Combine(Name, ID, State);
        public override bool Equals(object obj) => Equals(obj as Layer);
        public bool Equals(Layer other) => other != null && Name.Equals(other.Name) && ID.Equals(other.ID) && State.Equals(other.State);

        public static Layer Build(string name, long id, AnchorState state)
        {
            var r = new Layer
            {
                Name = name,
                ID = id,
                State = state
            };
            return r;
        }
    }
}
