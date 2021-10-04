using ProtoBuf;
using System;
using System.Drawing;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    //[ProtoContract(SkipConstructor = true)]
    //public class ComputeLayerBrainLink : IEquatable<ComputeLayerBrainLink>
    //{
    //    private ComputeLayerBrainLink(string name)
    //    {
    //        Name = name;
    //    }

    //    [ProtoMember(1)] public readonly string Name;

    //    public override int GetHashCode() => HashCode.Combine(Name);
    //    public override bool Equals(object? obj) => Equals(obj as ComputeLayerBrainLink);
    //    public bool Equals(ComputeLayerBrainLink? other) => other != null && Name.Equals(other.Name);

    //    public static ComputeLayerBrainLink Build(string name)
    //    {
    //        return new ComputeLayerBrainLink(name);
    //    }
    //}

    [ProtoContract(SkipConstructor = true)]
    public class OnlineInboundAdapterLink : IEquatable<OnlineInboundAdapterLink>
    {
        private OnlineInboundAdapterLink(string adapterName)
        {
            AdapterName = adapterName;
        }

        [ProtoMember(1)] public readonly string AdapterName;

        public override int GetHashCode() => HashCode.Combine(AdapterName);
        public override bool Equals(object? obj) => Equals(obj as OnlineInboundAdapterLink);
        public bool Equals(OnlineInboundAdapterLink? other) => other != null && AdapterName.Equals(other.AdapterName);

        public static OnlineInboundAdapterLink Build(string pluginName)
        {
            return new OnlineInboundAdapterLink(pluginName);
        }
    }

    [ProtoContract(SkipConstructor = true)]
    public class Layer : IEquatable<Layer>
    {
        private Layer(string id, string description, Color color, AnchorState state, string clConfig, OnlineInboundAdapterLink onlineInboundAdapterLink, string[] generators)
        {
            ID = id;
            Description = description;
            Color = color;
            State = state;
            CLConfig = clConfig;
            OnlineInboundAdapterLink = onlineInboundAdapterLink;
            Generators = generators;
        }

        [ProtoMember(1)] public readonly string ID;
        [ProtoMember(2)] public readonly string Description;
        [ProtoMember(3)] public readonly AnchorState State;
        [ProtoMember(4)] public readonly Color Color;
        [ProtoMember(5)] public readonly string CLConfig;
        [ProtoMember(6)] public readonly OnlineInboundAdapterLink OnlineInboundAdapterLink; // TODO: why are these links here? isn't a string reference enough?
        [ProtoMember(7)] public readonly string[] Generators;

        public override int GetHashCode() => HashCode.Combine(ID, Description, State, Color, CLConfig, OnlineInboundAdapterLink, Generators);
        public override bool Equals(object? obj) => Equals(obj as Layer);
        public bool Equals(Layer? other) => other != null && ID.Equals(other.ID) && Description.Equals(other.Description)
            && State.Equals(other.State) && CLConfig.Equals(other.CLConfig) && OnlineInboundAdapterLink.Equals(other.OnlineInboundAdapterLink) && Enumerable.SequenceEqual(Generators, other.Generators);

        public static Layer Build(string id, string description, Color color, AnchorState state, string clConfig, OnlineInboundAdapterLink onlineInboundAdapter, string[] generators)
        {
            return new Layer(id, description, color, state, clConfig, onlineInboundAdapter, generators);
        }
    }
}
