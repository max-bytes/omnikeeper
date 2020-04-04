using Landscape.Base.Entity;
using Landscape.Base.Model;
using LandscapeRegistry.Entity;
using LandscapeRegistry.Service;
using LandscapeRegistry.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class TraitModel
    {
        private readonly NpgsqlConnection conn;

        private ITraitsProvider TraitsProvider { get; set; }
        private readonly ICIModel ciModel;
        public TraitModel(ICIModel ciModel, ITraitsProvider traitsProvider, NpgsqlConnection connection)
        {
            TraitsProvider = traitsProvider;
            conn = connection;
            this.ciModel = ciModel;
        }

        public async Task<IEnumerable<EffectiveTrait>> CalculateEffectiveTraitsForCI(MergedCI ci, NpgsqlTransaction trans)
        {
            var traits = await TraitsProvider.GetTraits(trans);

            var ret = new List<EffectiveTrait>();
            foreach(var trait in traits.traits.Values)
            {
                var et = CalculateEffectiveTrait(trait, ci);
                if (et != null) ret.Add(et);
            }
            return ret;
        }

        public async Task<IEnumerable<(EffectiveTrait, MergedCI)>> FindCIsByTraitName(string traitName, LayerSet layerSet, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var traits = await TraitsProvider.GetTraits(trans);
            var trait = traits.traits.GetValueOrDefault(traitName);
            if (trait == null) return null; // trait not found by name
            return await FindCIsByTrait(trait, layerSet, trans, atTime);

        }

        public async Task<IEnumerable<(EffectiveTrait, MergedCI)>> FindCIsByTrait(Trait trait, LayerSet layerSet, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            // do a precursor filtering based on attribute names
            var requiredAttributeNames = trait.Attributes.Select(a => a.AttributeTemplate.Name);
            var candidateCIIDs = new List<string>();
            var tempLayersetTableName = await LayerSet.CreateLayerSetTempTable(layerSet, "temp_layerset", conn, trans);

            using (var command = new NpgsqlCommand(@$"
                select a.ci_id from
                (
                    select distinct on (inn.name, inn.ci_id) inn.name, inn.ci_id
                            from(select distinct on(ci_id, name, layer_id) * from
                                  attribute where timestamp <= @time_threshold and layer_id = ANY(@layer_ids)
                                     and name = ANY(@required_attributes)
                                     order by ci_id, name, layer_id, timestamp DESC
                    ) inn
                    inner join {tempLayersetTableName} ls ON inn.layer_id = ls.id -- inner join to only keep rows that are in the selected layers
                    where inn.state != 'removed'::attributestate -- remove entries from layers which' last item is deleted
                    order by inn.name, inn.ci_id, ls.order DESC
                ) a
                group by a.ci_id
                having count(a.ci_id) = cardinality(@required_attributes)", conn, trans))
            {
                command.Parameters.AddWithValue("time_threshold", atTime);
                command.Parameters.AddWithValue("layer_ids", layerSet.ToArray());
                command.Parameters.AddWithValue("required_attributes", requiredAttributeNames.ToArray());
                using var dr = command.ExecuteReader();

                while (dr.Read())
                {
                    var CIID = dr.GetString(0);
                    candidateCIIDs.Add(CIID);
                }
            }

            // now do a full pass to check which ci's REALLY fulfill the trait's requirements
            var cis = await ciModel.GetMergedCIs(layerSet, false, trans, atTime, candidateCIIDs);
            return cis.Select(ci => (effectiveTrait: CalculateEffectiveTrait(trait, ci), ci)).Where(t => t.effectiveTrait != null);
        }

        private EffectiveTrait CalculateEffectiveTrait(Trait trait, MergedCI ci)
        {
            var effectiveTraitAttributes = trait.Attributes.Select(ta =>
            {
                var foundAttribute = ci.MergedAttributes.FirstOrDefault(a => a.Attribute.Name == ta.AttributeTemplate.Name);
                var effectiveName = ta.AlternativeName;
                return (effectiveName, foundAttribute, checks: TemplateCheckService.PerAttributeTemplateChecks(foundAttribute, ta.AttributeTemplate));
            });

            var isTraitApplicable = effectiveTraitAttributes.All(t => t.checks.IsEmpty());

            if (isTraitApplicable)
                return EffectiveTrait.Build(trait, effectiveTraitAttributes.ToDictionary(t => t.effectiveName, t => t.foundAttribute));
            return null;
        }
    }
}
