﻿using Landscape.Base.Entity;
using Landscape.Base.Model;
using LandscapeRegistry.Service;
using LandscapeRegistry.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class TraitModel : ITraitModel
    {
        private readonly NpgsqlConnection conn;

        private ITraitsProvider TraitsProvider { get; set; }
        private readonly ICIModel ciModel;
        private readonly IRelationModel relationModel;
        public TraitModel(ICIModel ciModel, IRelationModel relationModel, ITraitsProvider traitsProvider, NpgsqlConnection connection)
        {
            TraitsProvider = traitsProvider;
            conn = connection;
            this.ciModel = ciModel;
            this.relationModel = relationModel;
        }

        public async Task<EffectiveTraitSet> CalculateEffectiveTraitSetForCI(MergedCI ci, NpgsqlTransaction trans)
        {
            var traits = await TraitsProvider.GetTraits(trans);

            var ret = new List<EffectiveTrait>();
            foreach (var trait in traits.Values)
            {
                var et = await CalculateEffectiveTrait(trait, ci, trans);
                if (et != null) ret.Add(et);
            }
            return EffectiveTraitSet.Build(ci, ret);
        }

        public async Task<IEnumerable<EffectiveTraitSet>> CalculateEffectiveTraitSetsForTraitName(string traitName, LayerSet layerSet, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            var traits = await TraitsProvider.GetTraits(trans);
            var trait = traits.GetValueOrDefault(traitName);
            if (trait == null) return null; // trait not found by name
            return await CalculateEffectiveTraitSetsForTrait(trait, layerSet, trans, atTime);

        }

        public async Task<IEnumerable<EffectiveTraitSet>> CalculateEffectiveTraitSetsForTrait(Trait trait, LayerSet layerSet, NpgsqlTransaction trans, DateTimeOffset atTime)
        {
            // do a precursor filtering based on required attribute names
            var requiredAttributeNames = trait.RequiredAttributes.Select(a => a.AttributeTemplate.Name);
            var candidateCIIDs = new List<Guid>();
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
                    var CIID = dr.GetGuid(0);
                    candidateCIIDs.Add(CIID);
                }
            }

            // now do a full pass to check which ci's REALLY fulfill the trait's requirements
            // TODO: performance improvement: use a function that works on a list of ci's, not check every ci on its own (and do N queries)
            var cis = await ciModel.GetMergedCIs(layerSet, false, trans, atTime, candidateCIIDs);
            var effectiveTraits = new List<EffectiveTraitSet>();
            foreach (var ci in cis)
            {
                var et = await CalculateEffectiveTrait(trait, ci, trans);
                if (et != null)
                    effectiveTraits.Add(EffectiveTraitSet.Build(ci, et));
            }
            return effectiveTraits;
        }

        private async Task<EffectiveTrait> CalculateEffectiveTrait(Trait trait, MergedCI ci, NpgsqlTransaction trans)
        {
            var relationsAndToCIs = (await RelationService.GetMergedForwardRelationsAndToCIs(ci, ciModel, relationModel, trans))
                .ToLookup(t => t.relation.PredicateID);

            var requiredEffectiveTraitAttributes = trait.RequiredAttributes.Select(ta =>
            {
                var traitAttributeIdentifier = ta.Identifier;
                var (foundAttribute, checks) = TemplateCheckService.CalculateTemplateErrorsAttribute(ci, ta.AttributeTemplate);
                return (traitAttributeIdentifier, foundAttribute, checks);
            });
            var requiredEffectiveTraitRelations = trait.RequiredRelations.Select(tr =>
            {
                var traitRelationIdentifier = tr.Identifier;
                var (foundRelations, checks) = TemplateCheckService.CalculateTemplateErrorsRelation(relationsAndToCIs, tr.RelationTemplate);
                return (traitRelationIdentifier, foundRelations, checks);
            });

            var isTraitApplicable = requiredEffectiveTraitAttributes.All(t => t.checks.Errors.IsEmpty()) && requiredEffectiveTraitRelations.All(t => t.checks.Errors.IsEmpty());



            if (isTraitApplicable)
            {
                // add optional traitAttributes
                var optionalEffectiveTraitAttributes = trait.OptionalAttributes.Select(ta =>
                {
                    var traitAttributeIdentifier = ta.Identifier;
                    var (foundAttribute, checks) = TemplateCheckService.CalculateTemplateErrorsAttribute(ci, ta.AttributeTemplate);
                    return (traitAttributeIdentifier, foundAttribute, checks);
                }).Where(t => t.checks.Errors.IsEmpty());

                return EffectiveTrait.Build(trait,
                    requiredEffectiveTraitAttributes.Concat(optionalEffectiveTraitAttributes).ToDictionary(t => t.traitAttributeIdentifier, t => t.foundAttribute),
                    requiredEffectiveTraitRelations.ToDictionary(t => t.traitRelationIdentifier, t => t.foundRelations));
            }
            return null;
        }
    }
}
