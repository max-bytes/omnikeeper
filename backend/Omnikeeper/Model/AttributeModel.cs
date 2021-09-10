using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Utils;
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
            for (var i = 0;i < layerIDs.Length;i++)// each (var (layerID, cis) in layeredAttributes)
            {
                var layerID = layerIDs[i];
                var cis = layeredAttributes[i];
                foreach (var ci in cis)
                {
                    var ciid = ci.Key;
                    if (compound.TryGetValue(ciid, out var existingAttributes))
                    {
                        foreach(var newAttribute in ci.Value)
                        {
                            if (existingAttributes.TryGetValue(newAttribute.Key, out var existingMergedAttribute))
                            {
                                existingAttributes[newAttribute.Key].LayerStackIDs.Add(layerID);
                            } else
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

        private IDictionary<Guid, MergedCIAttribute> MergeAttributes(IEnumerable<(IDictionary<Guid, CIAttribute> attributes, string layerID)> layeredAttributes)
        {
            var compound = new Dictionary<Guid, MergedCIAttribute>();
            foreach (var (cis, layerID) in layeredAttributes)
            {
                foreach (var ci in cis)
                {
                    var ciid = ci.Key;
                    if (compound.TryGetValue(ciid, out var existingMergedAttribute))
                    {
                        existingMergedAttribute.LayerStackIDs.Add(layerID);
                    }
                    else
                    {
                        compound.Add(ciid, new MergedCIAttribute(ci.Value, new List<string> { layerID }));
                    }
                }
            }
            return compound;
        }

        // strings must be a pre-sorted enumerable based on layer-sort
        private IDictionary<Guid, string> MergeStrings(IEnumerable<(IDictionary<Guid, string> strings, string layerID)> strings)
        {
            var ret = new Dictionary<Guid, string>();
            foreach (var g in strings)
            {
                var layerID = g.layerID;
                foreach (var kv in g.strings)
                {
                    ret.TryAdd(kv.Key, kv.Value);
                }
            }
            return ret;
        }

        public async Task<MergedCIAttribute?> GetMergedAttribute(string name, Guid ciid, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            return await _GetMergedAttribute(name, ciid, layers, trans, atTime, false);
        }
        public async Task<MergedCIAttribute?> GetFullBinaryMergedAttribute(string name, Guid ciid, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            return await _GetMergedAttribute(name, ciid, layers, trans, atTime, true);
        }

        public async Task<MergedCIAttribute?> _GetMergedAttribute(string name, Guid ciid, LayerSet layers, IModelContext trans, TimeThreshold atTime, bool fullBinary)
        {
            if (layers.IsEmpty)
                return null; // return empty, an empty layer list can never produce any attributes
            var attributes = new IDictionary<Guid, IDictionary<string, CIAttribute>>[layers.Length];
            var i = 0;
            foreach (var layerID in layers) // TODO: rework for GetAttribute() to support multi layers
            {
                CIAttribute? a;
                if (fullBinary)
                    a = await baseModel.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
                else
                    a = await baseModel.GetAttribute(name, ciid, layerID, trans, atTime);
                if (a != null)
                    attributes[i++] = new Dictionary<Guid, IDictionary<string, CIAttribute>>() { { ciid, new Dictionary<string, CIAttribute>() { { a.Name, a } } } };
                else
                    attributes[i++] = new Dictionary<Guid, IDictionary<string, CIAttribute>>();
            }

            var mergedAttributes = MergeAttributes(attributes, layers.LayerIDs);

            if (mergedAttributes.Count() > 1)
                throw new Exception("Should never happen!");

            // TODO: easier way? write a better suited MergeAttributes()?
            var ma = mergedAttributes.FirstOrDefault().Value?.Values.FirstOrDefault();
            // if the attribute is removed, we don't return it
            if (ma == null || ma.Attribute.State == AttributeState.Removed)
                return null;
            return ma;
        }

        public async Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetMergedAttributes(ICIIDSelection cs, LayerSet layers, IModelContext trans, TimeThreshold atTime, string? nameRegexFilter = null)
        {
            if (layers.IsEmpty)
                return ImmutableDictionary<Guid, IDictionary<string, MergedCIAttribute>>.Empty; // return empty, an empty layer list can never produce any attributes

            var attributes = await baseModel.GetAttributes(cs, layers.LayerIDs, returnRemoved: false, trans, atTime);

            var ret = MergeAttributes(attributes, layers.LayerIDs);

            return ret;
        }

        public async Task<IDictionary<Guid, MergedCIAttribute>> FindMergedAttributesByFullName(string name, ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            if (layers.IsEmpty)
                return ImmutableDictionary<Guid, MergedCIAttribute>.Empty; // return empty, an empty layer list can never produce any attributes

            var attributes = new (IDictionary<Guid, CIAttribute> attributes, string layerID)[layers.Length];
            var i = 0;
            foreach (var layerID in layers)
            {
                var la = await baseModel.FindAttributesByFullName(name, selection, layerID, trans, atTime);
                attributes[i++] = (la, layerID);
            }

            var mergedAttributes = MergeAttributes(attributes);

            return mergedAttributes;
        }

        public async Task<IDictionary<Guid, string>> GetMergedCINames(ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            if (layers.IsEmpty)
                return ImmutableDictionary<Guid, string>.Empty; // return empty, an empty layer list can never produce anything

            var names = new (IDictionary<Guid, string> names, string layerID)[layers.Length];
            var i = 0;
            foreach (var layerID in layers)
            {
                var la = await baseModel.GetCINames(selection, layerID, trans, atTime);
                names[i++] = (la, layerID);
            }

            var ret = MergeStrings(names);
            return ret;
        }

        public async Task<IDictionary<Guid, IDictionary<string, CIAttribute>>[]> GetAttributes(ICIIDSelection selection, string[] layerIDs, bool returnRemoved, IModelContext trans, TimeThreshold atTime, string? nameRegexFilter = null)
        {
            return await baseModel.GetAttributes(selection, layerIDs, returnRemoved, trans, atTime, nameRegexFilter);
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributesOfChangeset(Guid changesetID, IModelContext trans)
        {
            return await baseModel.GetAttributesOfChangeset(changesetID, trans);
        }

        public async Task<CIAttribute?> GetAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await baseModel.GetAttribute(name, ciid, layerID, trans, atTime);
        }
        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await baseModel.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<IDictionary<Guid, CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await baseModel.FindAttributesByFullName(name, selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Guid>> FindCIIDsWithAttribute(string name, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await baseModel.FindCIIDsWithAttribute(name, selection, layerID, trans, atTime);
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            return await baseModel.InsertAttribute(name, value, ciid, layerID, changeset, origin, trans);
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            return await baseModel.RemoveAttribute(name, ciid, layerID, changeset, origin, trans);
        }

        public async Task<IEnumerable<(Guid ciid, string fullName)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            return await baseModel.BulkReplaceAttributes(data, changeset, origin, trans);
        }

        public async Task<IDictionary<Guid, string>> GetCINames(ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await baseModel.GetCINames(selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<Guid>> FindCIIDsWithAttributeNameAndValue(string name, IAttributeValue value, ICIIDSelection selection, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await baseModel.FindCIIDsWithAttributeNameAndValue(name, value, selection, layerID, trans, atTime);
        }
    }
}
