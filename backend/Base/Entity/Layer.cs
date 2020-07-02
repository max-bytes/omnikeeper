using System;
using System.Drawing;

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

    public class OnlineInboundAdapter : IEquatable<OnlineInboundAdapter>
    {
        public string AdapterName { get; private set; }

        public override int GetHashCode() => HashCode.Combine(AdapterName);
        public override bool Equals(object obj) => Equals(obj as OnlineInboundAdapter);
        public bool Equals(OnlineInboundAdapter other) => other != null && AdapterName.Equals(other.AdapterName);

        public static OnlineInboundAdapter Build(string pluginName)
        {
            return new OnlineInboundAdapter
            {
                AdapterName = pluginName
            };
        }
    }

    public class Layer : IEquatable<Layer>
    {
        public string Name { get; private set; }
        public long ID { get; private set; }
        public AnchorState State { get; private set; }
        public Color Color { get; private set; }
        public ComputeLayerBrain ComputeLayerBrain { get; private set; }
        public OnlineInboundAdapter OnlineInboundAdapter { get; private set; }

        public override int GetHashCode() => HashCode.Combine(Name, ID, State, Color, ComputeLayerBrain, OnlineInboundAdapter);
        public override bool Equals(object obj) => Equals(obj as Layer);
        public bool Equals(Layer other) => other != null && Name.Equals(other.Name)
            && ID.Equals(other.ID) && State.Equals(other.State) && ComputeLayerBrain.Equals(other.ComputeLayerBrain) && OnlineInboundAdapter.Equals(other.OnlineInboundAdapter);

        public static Layer Build(string name, long id, Color color, AnchorState state, ComputeLayerBrain computeLayerBrain, OnlineInboundAdapter onlineInboundAdapter)
        {
            return new Layer
            {
                Name = name,
                ID = id,
                Color = color,
                State = state,
                ComputeLayerBrain = computeLayerBrain,
                OnlineInboundAdapter = onlineInboundAdapter
            };
        }
    }
}
