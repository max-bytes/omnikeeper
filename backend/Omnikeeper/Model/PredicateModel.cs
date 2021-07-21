using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    // TODO: think about caching?
    public class PredicateModel : IPredicateModel
    {
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;

        public PredicateModel(IBaseConfigurationModel baseConfigurationModel, IEffectiveTraitModel effectiveTraitModel)
        {
            this.baseConfigurationModel = baseConfigurationModel;
            this.effectiveTraitModel = effectiveTraitModel;
        }

        public async Task<Predicate> GetPredicate(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            var t = await TryToGetPredicate(id, timeThreshold, trans);
            if (t.Equals(default))
            {
                throw new Exception($"Could not find predicate with ID {id}");
            } else
            {
                return t.Item2;
            }
        }

        public async Task<(Guid,Predicate)> TryToGetPredicate(string id, TimeThreshold timeThreshold, IModelContext trans)
        {

            var traitForPredicates = CoreTraits.Predicate;
            // NOTE: we need to flatten the core trait first... is this the best way? Could we maybe also keep core traits as flattened already?
            var flattenedTraitForPredicates = RecursiveTraitService.FlattenSingleRecursiveTrait(traitForPredicates);

            // derive config layerset from base config
            var baseConfig = await baseConfigurationModel.GetConfigOrDefault(trans);
            var configLayerset = new LayerSet(baseConfig.ConfigLayerset);

            // TODO: better performance possible?
            var predicateCIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(flattenedTraitForPredicates, configLayerset, new AllCIIDsSelection(), trans, timeThreshold);

            var foundPredicateCI = predicateCIs.FirstOrDefault(pci => pci.Value.et.TraitAttributes["id"].Attribute.Value.Value2String() == id);
            if (!foundPredicateCI.Equals(default(KeyValuePair<Guid, (MergedCI ci, EffectiveTrait et)>)))
            {
                return (foundPredicateCI.Key, EffectiveTrait2Predicate(foundPredicateCI.Value.et));
            }
            return default;
        }

        private Predicate EffectiveTrait2Predicate(EffectiveTrait et)
        {
            var idA = et.TraitAttributes["id"];
            var predicateID = idA.Attribute.Value.Value2String();
            var wordingFromA = et.TraitAttributes["wordingFrom"];
            var wordingToA = et.TraitAttributes["wordingTo"];
            var wordingFrom = wordingFromA.Attribute.Value.Value2String();
            var wordingTo = wordingToA.Attribute.Value.Value2String();
            var constraints = PredicateConstraints.Default; // TODO
            return new Predicate(predicateID, wordingFrom, wordingTo, constraints);
        }

        public async Task<IDictionary<string, Predicate>> GetPredicates(IModelContext trans, TimeThreshold timeThreshold)
        {
            var traitForPredicates = CoreTraits.Predicate;
            // NOTE: we need to flatten the core trait first... is this the best way? Could we maybe also keep core traits as flattened already?
            var flattenedTraitForPredicates = RecursiveTraitService.FlattenSingleRecursiveTrait(traitForPredicates);

            // derive config layerset from base config
            var baseConfig = await baseConfigurationModel.GetConfigOrDefault(trans);
            var configLayerset = new LayerSet(baseConfig.ConfigLayerset);

            var predicateCIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(flattenedTraitForPredicates, configLayerset, new AllCIIDsSelection(), trans, timeThreshold);
            var ret = new Dictionary<string, Predicate>();
            foreach(var (_, predicateET) in predicateCIs.Values)
            {
                var p = EffectiveTrait2Predicate(predicateET);
                ret.Add(p.ID, p);
            }
            return ret;
        }
    }
}
