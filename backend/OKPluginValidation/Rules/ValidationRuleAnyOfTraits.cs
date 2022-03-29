using Newtonsoft.Json;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OKPluginValidation.Rules
{
    public class ValidationRuleAnyOfTraits : IValidationRule
    {
        private readonly ICIModel ciModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ITraitsProvider traitsProvider;

        public static string StaticName => typeof(ValidationRuleAnyOfTraits).Name;
        public string Name => StaticName;

        private class Config
        {
            public readonly string[] Layerset;
            public readonly string[] TraitIDs;

            public Config(string[] layerset, string[] traitIDs)
            {
                Layerset = layerset;
                this.TraitIDs = traitIDs;
            }

            public static readonly MyJSONSerializer<Config> Serializer = new MyJSONSerializer<Config>(() =>
            {
                var s = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.None
                };
                return s;
            });
        }

        public ValidationRuleAnyOfTraits(ITraitsProvider traitsProvider, ICIModel ciModel, IEffectiveTraitModel effectiveTraitModel)
        {
            this.traitsProvider = traitsProvider;
            this.ciModel = ciModel;
            this.effectiveTraitModel = effectiveTraitModel;
        }

        public async Task<IEnumerable<ValidationIssue>> PerformValidation(Validation validation, Guid validationCIID, IModelContext trans, TimeThreshold atTime)
        {
            Config parsedConfig;
            try
            {
                parsedConfig = Config.Serializer.Deserialize(validation.RuleConfig);
            }
            catch (Exception e)
            {
                throw new Exception("Could not parse config", e);
            }

            var layerset = new LayerSet(parsedConfig.Layerset);

            var traits = await traitsProvider.GetActiveTraitsByIDs(parsedConfig.TraitIDs, trans, atTime);
            var attributeSelection = AllAttributeSelection.Instance;

            var workCIs = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerset!, includeEmptyCIs: false, attributeSelection, trans, atTime);
            var cisThatDontHaveAnyTrait = effectiveTraitModel.FilterMergedCIsByTraits(workCIs, Enumerable.Empty<ITrait>(), traits.Values, layerset, trans, atTime);

            if (cisThatDontHaveAnyTrait.IsEmpty())
                return new ValidationIssue[0];

            var issueID = $"{Name}:{layerset}"; // TODO: name clashes possible? IDs need to be unique after all

            return new ValidationIssue[] { new ValidationIssue(issueID, $"CI does not have any of the traits {string.Join(", ", parsedConfig.TraitIDs)} in layerset {layerset}", cisThatDontHaveAnyTrait.Select(ci => ci.ID).ToArray(), validationCIID) };
        }
    }
}
