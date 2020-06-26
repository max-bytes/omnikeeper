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

    public class OnlineInboundLayerPlugin : IEquatable<OnlineInboundLayerPlugin>
    {
        public string PluginName { get; private set; }

        public override int GetHashCode() => HashCode.Combine(PluginName);
        public override bool Equals(object obj) => Equals(obj as OnlineInboundLayerPlugin);
        public bool Equals(OnlineInboundLayerPlugin other) => other != null && PluginName.Equals(other.PluginName);

        public static OnlineInboundLayerPlugin Build(string pluginName)
        {
            return new OnlineInboundLayerPlugin
            {
                PluginName = pluginName
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
        public OnlineInboundLayerPlugin OnlineInboundLayerPlugin { get; private set; }

        public override int GetHashCode() => HashCode.Combine(Name, ID, State, Color, ComputeLayerBrain, OnlineInboundLayerPlugin);
        public override bool Equals(object obj) => Equals(obj as Layer);
        public bool Equals(Layer other) => other != null && Name.Equals(other.Name)
            && ID.Equals(other.ID) && State.Equals(other.State) && ComputeLayerBrain.Equals(other.ComputeLayerBrain) && OnlineInboundLayerPlugin.Equals(other.OnlineInboundLayerPlugin);

        public static Layer Build(string name, long id, Color color, AnchorState state, ComputeLayerBrain computeLayerBrain, OnlineInboundLayerPlugin onlineInboundLayerPlugin)
        {
            return new Layer
            {
                Name = name,
                ID = id,
                Color = color,
                State = state,
                ComputeLayerBrain = computeLayerBrain,
                OnlineInboundLayerPlugin = onlineInboundLayerPlugin
            };
        }
    }
}
