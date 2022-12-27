using Omnikeeper.Base.Utils;
using System;

namespace Omnikeeper.Base.Entity
{
    [TraitEntity("__meta.config.layer_data", TraitOriginType.Core)]
    public class LayerData : TraitEntity, IEquatable<LayerData>
    {
        public LayerData() { LayerID = ""; Name = ""; Description = ""; Color = 0L; CLConfigID = ""; Generators = Array.Empty<string>(); State = AnchorState.Active.ToString(); }

        public LayerData(string layerID, string description, long color, string clConfigID, string[] generators, string state)
        {
            LayerID = layerID;
            Name = $"Layer-Data - {LayerID}";
            Description = description;
            Color = color;
            CLConfigID = clConfigID;
            Generators = generators;
            State = state;
        }

        [TraitAttribute("id", "layer_data.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public string LayerID;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public string Name;

        [TraitAttribute("description", "layer_data.description")]
        public string Description;

        [TraitAttribute("color", "layer_data.color")]
        public long Color;

        [TraitAttribute("cl_config_id", "layer_data.cl_config_id")]
        public string CLConfigID;

        [TraitAttribute("generators", "layer_data.generators")]
        public string[] Generators;

        [TraitAttribute("state", "layer_data.state")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        // TODO: constraint to enum values, support enums
        public string State;

        public override bool Equals(object? obj) => Equals(obj as LayerData);
        public bool Equals(LayerData? other)
        {
            return other != null && LayerID == other.LayerID && Name == other.Name && Description == other.Description && Color == other.Color && CLConfigID == other.CLConfigID &&
                Generators.NullRespectingSequenceEqual(other.Generators) && State == other.State;
        }
        public override int GetHashCode() => HashCode.Combine(LayerID, Name, Description, Color, CLConfigID, Generators, State);
    }
}
