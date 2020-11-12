using System;
using System.Drawing;

namespace Omnikeeper.Base.Entity
{
    public class ComputeLayerBrainLink : IEquatable<ComputeLayerBrainLink>
    {
        private ComputeLayerBrainLink(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public override int GetHashCode() => HashCode.Combine(Name);
        public override bool Equals(object? obj) => Equals(obj as ComputeLayerBrainLink);
        public bool Equals(ComputeLayerBrainLink? other) => other != null && Name.Equals(other.Name);

        public static ComputeLayerBrainLink Build(string name)
        {
            return new ComputeLayerBrainLink(name);
        }
    }

    public class OnlineInboundAdapterLink : IEquatable<OnlineInboundAdapterLink>
    {
        private OnlineInboundAdapterLink(string adapterName)
        {
            AdapterName = adapterName;
        }

        public string AdapterName { get; private set; }

        public override int GetHashCode() => HashCode.Combine(AdapterName);
        public override bool Equals(object? obj) => Equals(obj as OnlineInboundAdapterLink);
        public bool Equals(OnlineInboundAdapterLink? other) => other != null && AdapterName.Equals(other.AdapterName);

        public static OnlineInboundAdapterLink Build(string pluginName)
        {
            return new OnlineInboundAdapterLink(pluginName);
        }
    }

    public class Layer : IEquatable<Layer>
    {
        private Layer(string name, long iD, Color color, AnchorState state, ComputeLayerBrainLink computeLayerBrainLink, OnlineInboundAdapterLink onlineInboundAdapterLink)
        {
            Name = name;
            ID = iD;
            Color = color;
            State = state;
            ComputeLayerBrainLink = computeLayerBrainLink;
            OnlineInboundAdapterLink = onlineInboundAdapterLink;
        }

        public string Name { get; private set; }
        public long ID { get; private set; }
        public AnchorState State { get; private set; }
        public Color Color { get; private set; }
        public ComputeLayerBrainLink ComputeLayerBrainLink { get; private set; }
        public OnlineInboundAdapterLink OnlineInboundAdapterLink { get; private set; }

        public override int GetHashCode() => HashCode.Combine(Name, ID, State, Color, ComputeLayerBrainLink, OnlineInboundAdapterLink);
        public override bool Equals(object? obj) => Equals(obj as Layer);
        public bool Equals(Layer? other) => other != null && Name.Equals(other.Name)
            && ID.Equals(other.ID) && State.Equals(other.State) && ComputeLayerBrainLink.Equals(other.ComputeLayerBrainLink) && OnlineInboundAdapterLink.Equals(other.OnlineInboundAdapterLink);

        public static Layer Build(string name, long id, Color color, AnchorState state, ComputeLayerBrainLink computeLayerBrain, OnlineInboundAdapterLink onlineInboundAdapter)
        {
            return new Layer(name, id, color, state, computeLayerBrain, onlineInboundAdapter);
        }
    }
}
