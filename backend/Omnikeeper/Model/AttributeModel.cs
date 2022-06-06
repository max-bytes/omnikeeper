using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
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

        public AttributeModel(IBaseAttributeModel baseModel)
        {
            this.baseModel = baseModel;
        }

        // attributes must be a pre-sorted enumerable based on layer-sort
        private IDictionary<Guid, IDictionary<string, MergedCIAttribute>> MergeAttributes(IDictionary<Guid, IDictionary<string, CIAttribute>>[] layeredAttributes, string[] layerIDs)
        {
            // TODO: implement faster in case of single layer
            // TODO: think about implementing it faster by using a sparse attribute array instead of a dictionary

            var compound = new Dictionary<Guid, IDictionary<string, MergedCIAttribute>>();
            for (var i = 0; i < layerIDs.Length; i++)
            {
                var layerID = layerIDs[i];
                var cis = layeredAttributes[i];
                foreach (var ci in cis)
                {
                    var ciid = ci.Key;
                    if (compound.TryGetValue(ciid, out var existingAttributes))
                    {
                        foreach (var newAttribute in ci.Value)
                        {
                            if (existingAttributes.TryGetValue(newAttribute.Key, out var existingMergedAttribute))
                            {
                                existingAttributes[newAttribute.Key].LayerStackIDs.Add(layerID);
                            }
                            else
                            {
                                existingAttributes[newAttribute.Key] = new MergedCIAttribute(newAttribute.Value, new List<string> { layerID });
                            }
                        }
                    }
                    else
                    {
                        compound.Add(ciid, ci.Value.ToDictionary(a => a.Key, a => new MergedCIAttribute(a.Value, new List<string> { layerID })));
                    }
                }
            }
            return compound;
        }

        public async Task<MergedCIAttribute?> GetFullBinaryMergedAttribute(string name, Guid ciid, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            if (layers.IsEmpty)
                return null; // return empty, an empty layer list can never produce any attributes
            var attributes = new IDictionary<Guid, IDictionary<string, CIAttribute>>[layers.Length];
            var i = 0;
            foreach (var layerID in layers) // TODO: rework for GetFullBinaryAttribute() to support multi layers
            {
                CIAttribute? a = await baseModel.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
                if (a != null)
                    attributes[i++] = new Dictionary<Guid, IDictionary<string, CIAttribute>>() { { ciid, new Dictionary<string, CIAttribute>() { { a.Name, a } } } };
                else
                    attributes[i++] = new Dictionary<Guid, IDictionary<string, CIAttribute>>();
            }

            var mergedAttributes = MergeAttributes(attributes, layers.LayerIDs);

            if (mergedAttributes.Count() > 1)
                throw new Exception("Should never happen!");

            var ma = mergedAttributes.FirstOrDefault().Value?.Values.FirstOrDefault();
            return ma;
        }

        public async Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetMergedAttributes(ICIIDSelection cs, IAttributeSelection attributeSelection, LayerSet layers, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling)
        {
            if (layers.IsEmpty)
                return ImmutableDictionary<Guid, IDictionary<string, MergedCIAttribute>>.Empty; // return empty, an empty layer list can never produce any attributes

            var attributes = await baseModel.GetAttributes(cs, attributeSelection, layers.LayerIDs, trans: trans, atTime: atTime, generatedDataHandling);

            var ret = MergeAttributes(attributes, layers.LayerIDs);

            return ret;
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

        public async Task<int> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans,
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
            await baseModel.BulkUpdate(inserts, actualRemoves, data.LayerID, origin, changeset, trans);

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
                    await GetMergedAttributes(SpecificCIIDsSelection.Build(a.RelevantCIs), NamedAttributesSelection.Build(a.RelevantAttributes), layerset, trans, timeThreshold, GeneratedDataHandlingExclude.Instance),
                _ => throw new Exception("Unknown scope")
            };
        }
    }
}
