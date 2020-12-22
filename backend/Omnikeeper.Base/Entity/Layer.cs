using System;
using System.Drawing;

namespace Omnikeeper.Base.Entity
{
    [Serializable]
    public class ComputeLayerBrainLink : IEquatable<ComputeLayerBrainLink>
    {
        private ComputeLayerBrainLink(string name)
        {
            Name = name;
        }

        public readonly string Name;

        public override int GetHashCode() => HashCode.Combine(Name);
        public override bool Equals(object? obj) => Equals(obj as ComputeLayerBrainLink);
        public bool Equals(ComputeLayerBrainLink? other) => other != null && Name.Equals(other.Name);

        public static ComputeLayerBrainLink Build(string name)
        {
            return new ComputeLayerBrainLink(name);
        }
    }

    [Serializable]
    public class OnlineInboundAdapterLink : IEquatable<OnlineInboundAdapterLink>
    {
        private OnlineInboundAdapterLink(string adapterName)
        {
            AdapterName = adapterName;
        }

        public readonly string AdapterName;

        public override int GetHashCode() => HashCode.Combine(AdapterName);
        public override bool Equals(object? obj) => Equals(obj as OnlineInboundAdapterLink);
        public bool Equals(OnlineInboundAdapterLink? other) => other != null && AdapterName.Equals(other.AdapterName);

        public static OnlineInboundAdapterLink Build(string pluginName)
        {
            return new OnlineInboundAdapterLink(pluginName);
        }
    }

    [Serializable]
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

        public readonly string Name;
        public readonly long ID;
        public readonly AnchorState State;
        public readonly Color Color;
        public readonly ComputeLayerBrainLink ComputeLayerBrainLink;
        public readonly OnlineInboundAdapterLink OnlineInboundAdapterLink;

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
