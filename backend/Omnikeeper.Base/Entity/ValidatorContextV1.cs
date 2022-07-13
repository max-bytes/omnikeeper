using Omnikeeper.Base.Utils;
using System;
using System.Text.Json;

namespace Omnikeeper.Base.Entity
{
    [TraitEntity("__meta.config.validator_context", TraitOriginType.Core)]
    public class ValidatorContextV1 : TraitEntity, IEquatable<ValidatorContextV1>
    {
        private static JsonElementComparer jsonElementComparer = new JsonElementComparer();
        public ValidatorContextV1(string id, string validatorReference, JsonDocument config)
        {
            ID = id;
            ValidatorReference = validatorReference;
            Config = config;
            Name = $"Validator-Context {ID}";
        }

        public ValidatorContextV1()
        {
            ID = "";
            ValidatorReference = "";
            Config = JsonDocument.Parse("{}");
            Name = "";
        }

        [TraitAttribute("id", "validator_context.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitAttributeValueConstraintTextRegex(IDValidations.ValidatorContextIDRegexString, IDValidations.ValidatorContextIDRegexOptions)]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("validator_reference", "validator_context.validator_reference")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public string ValidatorReference;

        [TraitAttribute("config", "validator_context.config")]
        public JsonDocument Config;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public string Name;

        public override bool Equals(object? obj) => Equals(obj as ValidatorContextV1);
        public bool Equals(ValidatorContextV1? other)
        {
            return other != null && ID == other.ID &&
                   ValidatorReference == other.ValidatorReference &&
                   Name == other.Name &&
                   jsonElementComparer.Equals(Config.RootElement, other.Config.RootElement);
        }
        public override int GetHashCode() => HashCode.Combine(ID, ValidatorReference, Config, Name);
    }
}

