using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OKPluginValidation.Validation;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Validation.Rules
{
    public class ValidationRuleNamedCI : IValidationRule
    {
        private readonly ICIModel ciModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ITraitsProvider traitsProvider;
        private readonly IBaseAttributeModel baseAttributeModel;

        public static string StaticName => typeof(ValidationRuleNamedCI).Name;
        public string Name => StaticName;

        private class Config
        {
            public readonly string[] Layerset;

            public Config(string[] layerset)
            {
                Layerset = layerset;
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

        public ValidationRuleNamedCI(ITraitsProvider traitsProvider, IBaseAttributeModel baseAttributeModel, ICIModel ciModel, IEffectiveTraitModel effectiveTraitModel)
        {
            this.traitsProvider = traitsProvider;
            this.baseAttributeModel = baseAttributeModel;
            this.ciModel = ciModel;
            this.effectiveTraitModel = effectiveTraitModel;
        }

        public async Task<IEnumerable<ValidationIssue>> PerformValidation(OKPluginValidation.Validation.Validation validation, Guid validationCIID, IModelContext trans, TimeThreshold atTime)
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

            var attributeSelection = NamedAttributesSelection.Build(ICIModel.NameAttribute); // This is weird... it seems like we need to fetch at least ONE attribute otherwise, it's all empty.. which makes sense, but still...

            var traits = await traitsProvider.GetActiveTraitsByIDs(new string[] { CoreTraits.Named.ID }, trans, atTime);
            var workCIs = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerset!, includeEmptyCIs: true, attributeSelection, trans, atTime);
            var unnamedCIs = effectiveTraitModel.FilterMergedCIsByTraits(workCIs, Enumerable.Empty<ITrait>(), traits.Values, layerset, trans, atTime);

            var unnamedCISelection = SpecificCIIDsSelection.Build(unnamedCIs.Select(ci => ci.ID).ToHashSet());
            var nonEmptyButUnnamedCIIDs = await baseAttributeModel.GetCIIDsWithAttributes(unnamedCISelection, layerset.LayerIDs, trans, atTime);

            if (nonEmptyButUnnamedCIIDs.IsEmpty())
                return new ValidationIssue[0];

            var issueID = $"{Name}:{layerset}"; // TODO: name clashes possible? IDs need to be unique after all

            return new ValidationIssue[] { new ValidationIssue(issueID, $"CI unnamed in layerset {layerset}", nonEmptyButUnnamedCIIDs.ToArray(), validationCIID) };
        }
    }
}
