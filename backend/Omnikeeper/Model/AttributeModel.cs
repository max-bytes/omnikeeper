using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class AttributeModel : IAttributeModel
    {
        private readonly IBaseAttributeModel baseModel;
        private readonly Func<IEffectiveGeneratorProvider> effectiveGeneratorProviderFunc;

        public AttributeModel(IBaseAttributeModel baseModel, Func<IEffectiveGeneratorProvider> effectiveGeneratorProviderFunc)
        {
            this.baseModel = baseModel;
            this.effectiveGeneratorProviderFunc = effectiveGeneratorProviderFunc;
        }

        private async Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> MergeAttributes(IAsyncEnumerable<CIAttribute>[] layeredAttributes, string[] layerIDs, ICIIDSelection ciSelection, IAttributeSelection attributeSelection, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling)
        {
            // TODO: implement faster in case of single layer?
            // TODO: think about implementing it faster by using a sparse attribute array instead of a dictionary

            var compound = new Dictionary<Guid, IDictionary<string, MergedCIAttribute>>();
            for (var i = 0; i < layerIDs.Length; i++)
            {
                var layerID = layerIDs[i];
                var attributes = layeredAttributes[i];
                await foreach (var newAttribute in attributes)
                {
                    if (compound.TryGetValue(newAttribute.CIID, out var existingAttributes))
                    {
                        if (existingAttributes.TryGetValue(newAttribute.Name, out var existingMergedAttribute))
                        {
                            existingAttributes[newAttribute.Name].LayerStackIDs.Add(layerID);
                        }
                        else
                        {
                            existingAttributes[newAttribute.Name] = new MergedCIAttribute(newAttribute, new List<string> { layerID });
                        }
                    }
                    else
                    {
                        compound.Add(newAttribute.CIID, new Dictionary<string, MergedCIAttribute>() { { newAttribute.Name, new MergedCIAttribute(newAttribute, new List<string> { layerID }) } });
                    }
                }

                // NOTE: generated attributes can read from same layer and from layers below, which is weirdly inconsistent because when generators request additional attributes,
                // they only fetch them from the current layer
                // TODO: find a better way to handle all this; ideally, generators should be able to look at and request additional attributes from all selected layers
                switch (generatedDataHandling)
                {
                    case GeneratedDataHandlingExclude:
                        break;
                    case GeneratedDataHandlingInclude:
                        // TODO: maybe we can find an efficient way to not generate attributes that are guaranteed to be hidden by a higher layer anyway
                        var resolver = new GeneratorAttributeResolver();
                        var generatorSelection = new GeneratorSelectionAll();
                        var effectiveGeneratorProvider = effectiveGeneratorProviderFunc();

                        // calculate effective generators
                        // TODO: single layer handling
                        var egis = await effectiveGeneratorProvider.GetEffectiveGenerators(new string[] { layerID }, generatorSelection, attributeSelection, trans, atTime);

                        // bail early if there are no egis
                        if (egis.All(egi => egi.IsEmpty()))
                            break;

                        // we need to potentially extend the attributeSelection so that it contains all attributes necessary to resolve the generated attributes
                        // the caller is allowed to not know or care about generated attributes and their requirements, so we need to extend here
                        // and also (for the return structure) ignore any additionally fetched attributes that were only fetched to calculate the generated attributes
                        var additionalAttributeNames = attributeSelection switch
                        {
                            NamedAttributesSelection n => CalculateAdditionalRequiredDependentAttributes(egis, attributeSelection).ToImmutableHashSet(),
                            AllAttributeSelection _ => ImmutableHashSet<string>.Empty, // we are fetching all attributes anyway, no need to add additional attributes
                            NoAttributesSelection _ => ImmutableHashSet<string>.Empty, // no attributes necessary
                            _ => throw new Exception("Invalid attribute selection encountered"),
                        };
                        var additionalAttributes = (additionalAttributeNames.Count > 0) ?
                            baseModel.GetAttributes(ciSelection, NamedAttributesSelection.Build(additionalAttributeNames), layerID, trans, atTime) :
                            AsyncEnumerable.Empty<CIAttribute>();

                        var additionalAttributeLookup = await additionalAttributes.ToLookupAsync(a => a.CIID);
                        foreach (var egi in egis[0])
                        {
                            foreach(var ciid in compound.Keys.Union(additionalAttributeLookup.Select(g => g.Key)))
                            {
                                var additionals = additionalAttributeLookup[ciid];
                                var existing = compound.GetOrWithClass(ciid, null);
                                var generatedAttribute = resolver.Resolve(existing != null ? existing.Values.Select(ma => ma.Attribute) : ImmutableList<CIAttribute>.Empty, additionals, ciid, layerID, egi);
                                if (generatedAttribute != null)
                                {
                                    if (attributeSelection.ContainsAttribute(generatedAttribute)) // apply attribute selection to generated attribute
                                    {
                                        if (existing != null)
                                        {
                                            if (existing.TryGetValue(egi.AttributeName, out var _))
                                            {
                                                existing[egi.AttributeName].LayerStackIDs.Add(layerID);
                                            }
                                            else
                                            {
                                                // TODO: we are currently overwriting regular attributes with generated attributes... decide if that is the correct approach
                                                existing[egi.AttributeName] = new MergedCIAttribute(generatedAttribute, new List<string> { layerID });
                                            }
                                        }
                                        else
                                        {
                                            // NOTE: CI is empty (=does not contain any attributes) in the base data, add it and add the generated attribute in there
                                            compound[ciid] = new Dictionary<string, MergedCIAttribute>() { { egi.AttributeName, new MergedCIAttribute(generatedAttribute, new List<string> { layerID }) } };
                                        }
                                    }
                                }
                            }
                        }

                        break;
                    default:
                        throw new Exception("Unknown generated-data-handling detected");
                }
            }
            return compound;
        }

        private ISet<string> CalculateAdditionalRequiredDependentAttributes(IEnumerable<GeneratorV1>[] egis, IAttributeSelection baseAttributeSelection)
        {
            var ret = new HashSet<string>();
            for (int i = 0; i < egis.Length; i++)
            {
                foreach (var egi in egis[i])
                    ret.UnionWith(egi.Template.UsedAttributeNames.Where(name => !baseAttributeSelection.ContainsAttributeName(name)));
            }
            return ret;
        }

        public async Task<MergedCIAttribute?> GetFullBinaryMergedAttribute(string name, Guid ciid, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            if (layers.IsEmpty)
                return null; // return empty, an empty layer list can never produce any attributes
            CIAttribute? attribute = null;
            var layerStackIDs = new List<string>();

            foreach (var layerID in layers) // TODO: rework for GetFullBinaryAttribute() to support multi layers
            {
                CIAttribute? a = await baseModel.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
                if (a != null)
                {
                    if (attribute == null)
                        attribute = a;
                    layerStackIDs.Add(layerID);
                }
            }

            if (attribute == null)
                return null;
            return new MergedCIAttribute(attribute, layerStackIDs);
        }

        public async Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetMergedAttributes(ICIIDSelection cs, IAttributeSelection attributeSelection, LayerSet layers, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling)
        {
            if (layers.IsEmpty)
                return ImmutableDictionary<Guid, IDictionary<string, MergedCIAttribute>>.Empty; // return empty, an empty layer list can never produce any attributes

            var attributes = layers.Select(layerID => baseModel.GetAttributes(cs, attributeSelection, layerID, trans, atTime)).ToArray();

            return await MergeAttributes(attributes, layers.LayerIDs, cs, attributeSelection, trans, atTime, generatedDataHandling);
        }

        // NOTE: this bulk operation DOES check if the attributes that are inserted are "unique":
        // it is not possible to insert the "same" attribute (same ciid, name and layer) multiple times when using this preparation method
        // if this operation detects a duplicate, an exception is thrown;
        // the caller is responsible for making sure there are no duplicates
        private async Task<(
            IList<(Guid ciid, string fullName, IAttributeValue value, Guid? existingAttributeID, Guid newAttributeID)> inserts,
            IDictionary<string, CIAttribute> outdatedAttributes
            )> PrepareForBulkUpdate<F>(IBulkCIAttributeData<F> data, IModelContext trans, TimeThreshold readTS)
        {
            // consider ALL relevant attributes as outdated first
            var outdatedAttributes = (await GetAttributesInScope(data, new LayerSet(data.LayerID), trans, readTS))
                .SelectMany(t => t.Value.Select(tt => tt.Value.Attribute)).ToDictionary(a => a.InformationHash); // TODO: slow?

            var actualInserts = new List<(Guid ciid, string fullName, IAttributeValue value, Guid? existingAttributeID, Guid newAttributeID)>();
            var informationHashesToInsert = new HashSet<string>();
            foreach (var fragment in data.Fragments)
            {
                var fullName = data.GetFullName(fragment);
                var ciid = data.GetCIID(fragment);
                var value = data.GetValue(fragment);

                var informationHash = CIAttribute.CreateInformationHash(fullName, ciid);
                if (informationHashesToInsert.Contains(informationHash))
                {
                    throw new Exception($"Duplicate attribute fragment detected! Bulk insertion does not support duplicate attributes; attribute name: {fullName}, ciid: {ciid}, value: {value.Value2String()}");
                }
                informationHashesToInsert.Add(informationHash);

                // remove the current attribute from the list of attributes to remove
                outdatedAttributes.Remove(informationHash, out var currentAttribute);

                // handle equality case, also think about what should happen if a different user inserts the same data
                if (currentAttribute != null && currentAttribute.Value.Equals(value))
                    continue;

                actualInserts.Add((ciid, fullName, value, currentAttribute?.ID, Guid.NewGuid()));
            }

            return (actualInserts, outdatedAttributes);
        }

        public async Task<int> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, IModelContext trans,
            IMaskHandlingForRemoval maskHandling, IOtherLayersValueHandling otherLayersValueHandling)
        {
            var readTS = changeset.TimeThreshold;

            var (inserts, outdatedAttributes) = await PrepareForBulkUpdate(data, trans, readTS);

            var informationHashesToInsert = data.Fragments.Select(f => CIAttribute.CreateInformationHash(data.GetFullName(f), data.GetCIID(f))).ToHashSet();

            // mask-based changes to inserts and removals
            // depending on mask-handling, calculate attribute that are potentially "maskable" in below layers
            var maskableAttributesInBelowLayers = new Dictionary<string, (Guid ciid, string name)>();
            switch (maskHandling)
            {
                case MaskHandlingForRemovalApplyMaskIfNecessary n:
                    maskableAttributesInBelowLayers = (await GetAttributesInScope(data, new LayerSet(n.ReadLayersBelowWriteLayer), trans, readTS))
                    .SelectMany(t => t.Value.Select(tt => tt.Value.Attribute))
                    .GroupBy(t => t.InformationHash)
                    .Where(g => !informationHashesToInsert.Contains(g.Key)) // if we are already inserting this attribute, we definitely do not want to mask it
                    .ToDictionary(g => g.Key, g => (g.First().CIID, g.First().Name));
                    break;
                case MaskHandlingForRemovalApplyNoMask _:
                    // no operation necessary
                    break;
                default:
                    throw new Exception("Invalid mask handling");
            }
            // reduce the actual removes by looking at maskable attributes, replacing the removes with masks if necessary
            foreach (var kv in maskableAttributesInBelowLayers)
            {
                var ih = kv.Key;

                if (outdatedAttributes.TryGetValue(ih, out var outdatedAttribute))
                {
                    // the attribute exists in the write-layer AND is actually outdated AND needs to be masked -> mask it, instead of removing it
                    outdatedAttributes.Remove(ih);
                    inserts.Add((outdatedAttribute.CIID, outdatedAttribute.Name, AttributeScalarValueMask.Instance, outdatedAttribute.ID, Guid.NewGuid()));
                }
                else
                {
                    // the attribute exists only in the layers below -> mask it
                    inserts.Add((kv.Value.ciid, kv.Value.name, AttributeScalarValueMask.Instance, null, Guid.NewGuid()));
                }
            }

            // build removal-list
            var actualRemoves = outdatedAttributes.Values.Select(a => (a.CIID, a.Name, a.Value, existingAttributeID: a.ID, newAttributeID: Guid.NewGuid())).ToList();

            // other-layers-value handling
            switch (otherLayersValueHandling)
            {
                case OtherLayersValueHandlingTakeIntoAccount t:
                    // fetch attributes in layerset excluding write layer; if value is same as value that we want to write -> instead of write -> no-op or even delete
                    var existingAttributesInOtherLayers = await GetAttributesInScope(data, new LayerSet(t.ReadLayersWithoutWriteLayer), trans, readTS);
                    for (var i = inserts.Count - 1; i >= 0; i--)
                    {
                        var insert = inserts[i];
                        if (existingAttributesInOtherLayers.TryGetValue(insert.ciid, out var a))
                        {
                            if (a.TryGetValue(insert.fullName, out var aa))
                            {
                                if (aa.Attribute.Value.Equals(insert.value))
                                {
                                    inserts.RemoveAt(i);

                                    // in case there is an attribute there already, we actually remove it because the other layers provide the same attribute with the same value 
                                    if (insert.existingAttributeID.HasValue)
                                    {
                                        actualRemoves.Add((insert.ciid, insert.fullName, aa.Attribute.Value, insert.existingAttributeID.Value, Guid.NewGuid()));
                                    }
                                }
                            }
                        }
                    }
                    break;
                case OtherLayersValueHandlingForceWrite _:
                    // no operation necessary
                    break;
                default:
                    throw new Exception("Invalid other-layers-value handling");
            }

            // perform updates in bulk
            await baseModel.BulkUpdate(inserts, actualRemoves, data.LayerID, changeset, trans);

            return inserts.Count + actualRemoves.Count;
        }

        private async Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetAttributesInScope<F>(IBulkCIAttributeData<F> data, LayerSet layerset, IModelContext trans, TimeThreshold timeThreshold)
        {
            return data switch
            {
                BulkCIAttributeDataLayerScope _ => 
                    await GetMergedAttributes(AllCIIDsSelection.Instance, AllAttributeSelection.Instance, layerset, trans, timeThreshold, GeneratedDataHandlingExclude.Instance),
                BulkCIAttributeDataCIScope d =>
                    await GetMergedAttributes(SpecificCIIDsSelection.Build(d.CIID), AllAttributeSelection.Instance, layerset, trans: trans, atTime: timeThreshold, GeneratedDataHandlingExclude.Instance),
                BulkCIAttributeDataCIAndAttributeNameScope a =>
                    await GetMergedAttributes(a.RelevantCIs, a.RelevantAttributes, layerset, trans, timeThreshold, GeneratedDataHandlingExclude.Instance),
                _ => throw new Exception("Unknown scope")
            };
        }
    }
}
