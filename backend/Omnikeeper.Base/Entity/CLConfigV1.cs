using Omnikeeper.Base.Utils;
using System;
using System.Text.Json;

namespace Omnikeeper.Base.Entity
{
    [TraitEntity("__meta.config.cl_config", TraitOriginType.Core)]
    public class CLConfigV1 : TraitEntity, IEquatable<CLConfigV1>
    {
        public CLConfigV1(string id, string clBrainReference, JsonDocument clBrainConfig)
        {
            ID = id;
            CLBrainReference = clBrainReference;
            CLBrainConfig = clBrainConfig;
            Name = $"CL-Config {ID}";
        }

        public CLConfigV1()
        {
            ID = "";
            CLBrainReference = "";
            CLBrainConfig = JsonDocument.Parse("{}");
            Name = "";
        }

        [TraitAttribute("id", "cl_config.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitAttributeValueConstraintTextRegex(IDValidations.CLConfigIDRegexString, IDValidations.CLConfigIDRegexOptions)]
        [TraitEntityID]
        public readonly string ID;

        [TraitAttribute("cl_brain_reference", "cl_config.cl_brain_reference")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string CLBrainReference;

        [TraitAttribute("cl_brain_config", "cl_config.cl_brain_config")]
        public readonly JsonDocument CLBrainConfig;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Name;

        public override bool Equals(object? obj) => Equals(obj as CLConfigV1);
        public bool Equals(CLConfigV1? other)
        {
            return other != null && ID == other.ID &&
                   CLBrainReference == other.CLBrainReference &&
                   Name == other.Name &&
                   CLBrainConfig.RootElement.ToString() == other.CLBrainConfig.RootElement.ToString(); // TODO: implement proper deep equality
        }
        public override int GetHashCode() => HashCode.Combine(ID, CLBrainReference, CLBrainConfig, Name);
    }
}

