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
        private readonly ICISearchModel ciSearchModel;

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

        public ValidationRuleNamedCI(ICISearchModel ciSearchModel)
        {
            this.ciSearchModel = ciSearchModel;
        }

        public async Task<IEnumerable<ValidationIssue>> PerformValidation(JObject config, IModelContext trans, TimeThreshold atTime)
        {
            Config parsedConfig;
            try
            {
                parsedConfig = Config.Serializer.Deserialize(config);
            }
            catch (Exception e)
            {
                throw new Exception("Could not parse config", e);
            }

            var layerset = new LayerSet(parsedConfig.Layerset);

            var attributeSelection = NamedAttributesSelection.Build(ICIModel.NameAttribute); // This is weird... it seems like we need to fetch at least ONE attribute otherwise, it's all empty.. which makes sense, but still...
            var unnamedCIs = await ciSearchModel.SearchForMergedCIsByTraits(new AllCIIDsSelection(), attributeSelection, new string[0], new string[] { CoreTraits.Named.ID, TraitEmpty.StaticID }, layerset, trans, atTime);

            if (unnamedCIs.IsEmpty())
                return new ValidationIssue[0];

            var issueID = $"{Name}:{layerset}"; // TODO: name clashes possible? IDs need to be unique after all

            return new ValidationIssue[] { new ValidationIssue(issueID, $"CI unnamed in layerset {layerset}", unnamedCIs.Select(ci => ci.ID).ToArray()) };
        }
    }
}
