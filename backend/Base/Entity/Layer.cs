using System;

namespace Landscape.Base.Entity
{
    public class ComputeLayerBrain : IEquatable<ComputeLayerBrain>
    {
        public string Name { get; private set; }

        public override int GetHashCode() => HashCode.Combine(Name);
        public override bool Equals(object obj) => Equals(obj as ComputeLayerBrain);
        public bool Equals(ComputeLayerBrain other) => other != null && Name.Equals(other.Name);

        public static ComputeLayerBrain Build(string name)
        {
            return new ComputeLayerBrain
            {
                Name = name
            };
        }
    }

    public class Layer : IEquatable<Layer>
    {
        public string Name { get; private set; }
        public long ID { get; private set; }
        public AnchorState State { get; private set; }
        public ComputeLayerBrain ComputeLayerBrain { get; private set; }

        public override int GetHashCode() => HashCode.Combine(Name, ID, State);
        public override bool Equals(object obj) => Equals(obj as Layer);
        public bool Equals(Layer other) => other != null && Name.Equals(other.Name)
            && ID.Equals(other.ID) && State.Equals(other.State) && ComputeLayerBrain.Equals(other.ComputeLayerBrain);

        public static Layer Build(string name, long id, AnchorState state, ComputeLayerBrain computeLayerBrain)
        {
            return new Layer
            {
                Name = name,
                ID = id,
                State = state,
                ComputeLayerBrain = computeLayerBrain
            };
        }
    }
}
