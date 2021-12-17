using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Entity
{
    [TraitEntity("__meta.config.layer_data", TraitOriginType.Core)]
    public class LayerData : TraitEntity, IEquatable<LayerData>
    {
        public LayerData() { LayerID = ""; Name = ""; Description = ""; Color = 0L; CLConfigID = ""; Generators = Array.Empty<string>(); OIAReference = ""; State = AnchorState.Active.ToString(); }

        public LayerData(string layerID, string description, long color, string clConfigID, string[] generators, string oiaReference, string state)
        {
            LayerID = layerID;
            Name = $"Layer-Data - {LayerID}";
            Description = description;
            Color = color;
            CLConfigID = clConfigID;
            Generators = generators;
            OIAReference = oiaReference;
            State = state;
        }

        [TraitAttribute("id", "layer_data.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string LayerID;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Name;

        [TraitAttribute("description", "layer_data.description")]
        public readonly string Description;

        [TraitAttribute("color", "layer_data.color")]
        public readonly long Color;

        [TraitAttribute("cl_config_id", "layer_data.cl_config_id")]
        public readonly string CLConfigID;

        [TraitAttribute("generators", "layer_data.generators")]
        public readonly string[] Generators;

        [TraitAttribute("oia_reference", "layer_data.oia_reference")]
        public readonly string OIAReference;

        [TraitAttribute("state", "layer_data.state")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        // TODO: constraint to enum values, support enums
        public readonly string State;

        public override bool Equals(object? obj) => Equals(obj as LayerData);
        public bool Equals(LayerData? other)
        {
            return other != null && LayerID == other.LayerID && Name == other.Name && Description == other.Description && Color == other.Color && CLConfigID == other.CLConfigID && 
                Generators.NullRespectingSequenceEqual(other.Generators) && OIAReference == other.OIAReference && State == other.State;
        }
        public override int GetHashCode() => HashCode.Combine(LayerID, Name, Description, Color, CLConfigID, Generators, OIAReference, State);
    }
}
