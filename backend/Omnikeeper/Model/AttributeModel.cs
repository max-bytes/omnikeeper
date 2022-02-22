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

        public async Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetMergedAttributes(ICIIDSelection cs, IAttributeSelection attributeSelection, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            if (layers.IsEmpty)
                return ImmutableDictionary<Guid, IDictionary<string, MergedCIAttribute>>.Empty; // return empty, an empty layer list can never produce any attributes

            var attributes = await baseModel.GetAttributes(cs, attributeSelection, layers.LayerIDs, trans: trans, atTime: atTime);

            var ret = MergeAttributes(attributes, layers.LayerIDs);

            return ret;
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            return await baseModel.InsertAttribute(name, value, ciid, layerID, changeset, origin, trans);
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans, IMaskHandlingForRemoval maskHandling)
        {
            switch (maskHandling)
            {
                case MaskHandlingForRemovalApplyMaskIfNecessary n:
                    var attributeRemaining = await GetMergedAttributes(SpecificCIIDsSelection.Build(ciid), NamedAttributesSelection.Build(name), new LayerSet(n.ReadLayersBelowWriteLayer), trans, changeset.TimeThreshold);
                    if (attributeRemaining.TryGetValue(ciid, out var aa) && aa.ContainsKey(name))
                    { // attribute exists in lower layers, mask it
                        // NOTE: if the current attribute is already a mask, the InsertAttribute detects this and the operation becomes a NO-OP
                        return await baseModel.InsertAttribute(name, AttributeScalarValueMask.Instance, ciid, layerID, changeset, origin, trans);
                    }
                    else
                    {
                        // TODO: how should we handle the case when the attribute we try to delete is already a mask? -> NO-OP? or delete the mask? make it configurable?
                        // currently any mask is removed as well
                        return await baseModel.RemoveAttribute(name, ciid, layerID, changeset, origin, trans);
                    }
                case MaskHandlingForRemovalApplyNoMask _:
                    return await baseModel.RemoveAttribute(name, ciid, layerID, changeset, origin, trans);
                default:
                    throw new Exception("Invalid mask handling");
            }
        }

        public async Task<bool> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans, IMaskHandlingForRemoval maskHandling)
        {
            var readTS = changeset.TimeThreshold;

            var (inserts, outdatedAttributes) = await baseModel.PrepareForBulkUpdate(data, trans, readTS);

            var informationHashesToInsert = data.Fragments.Select(f => CIAttribute.CreateInformationHash(data.GetFullName(f), data.GetCIID(f))).ToHashSet();

            // mask-based changes to inserts and removals
            // depending on mask-handling, calculate attribute that are potentially "maskable" in below layers
            var maskableAttributesInBelowLayers = new Dictionary<string, (Guid ciid, string name)>();
            switch (maskHandling)
            {
                case MaskHandlingForRemovalApplyMaskIfNecessary n:

                    maskableAttributesInBelowLayers = (data switch
                    {
                        BulkCIAttributeDataLayerScope d => (d.NamePrefix.IsEmpty()) ?
                                (await baseModel.GetAttributes(new AllCIIDsSelection(), AllAttributeSelection.Instance, n.ReadLayersBelowWriteLayer, trans, readTS)) :
                                (await baseModel.GetAttributes(new AllCIIDsSelection(), new RegexAttributeSelection($"^{d.NamePrefix}"), n.ReadLayersBelowWriteLayer, trans, readTS)),
                        BulkCIAttributeDataCIScope d =>
                            await baseModel.GetAttributes(SpecificCIIDsSelection.Build(d.CIID), AllAttributeSelection.Instance, n.ReadLayersBelowWriteLayer, trans: trans, atTime: readTS),
                        BulkCIAttributeDataCIAndAttributeNameScope a =>
                            await baseModel.GetAttributes(SpecificCIIDsSelection.Build(a.RelevantCIs), NamedAttributesSelection.Build(a.RelevantAttributes), n.ReadLayersBelowWriteLayer, trans, readTS),
                        _ => throw new Exception("Unknown scope")
                    })
                    .SelectMany(t => t.Values.SelectMany(tt => tt.Values))
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

            // build final removal-list
            var actualRemoves = outdatedAttributes.Values.Select(a => (a.CIID, a.Name, a.Value, a.ID, Guid.NewGuid())).ToList();

            // perform updates in bulk
            var (changed, _) = await baseModel.BulkUpdate(inserts, actualRemoves, data.LayerID, origin, changeset, trans);

            return changed;
        }
    }
}
