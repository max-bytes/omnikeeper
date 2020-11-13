﻿using Npgsql;
using Omnikeeper.Base.Entity;
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

        private IEnumerable<MergedCIAttribute> MergeAttributes(IEnumerable<(CIAttribute attribute, long layerID)> attributes, LayerSet layers)
        {
            var compound = new Dictionary<(Guid ciid, string name), SortedList<int, (CIAttribute attribute, long layerID)>>();

            foreach (var (attribute, layerID) in attributes)
            {
                var layerSortOrder = layers.GetOrder(layerID);

                compound.AddOrUpdate((attribute.CIID, attribute.Name),
                    () => new SortedList<int, (CIAttribute attribute, long layerID)>() { { layerSortOrder, (attribute, layerID) } },
                    (old) => { old.Add(layerSortOrder, (attribute, layerID)); return old; }
                );
            }

            return compound.Select(t => new MergedCIAttribute(t.Value.First().Value.attribute, layerStackIDs: t.Value.Select(tt => tt.Value.layerID).Reverse().ToArray()));
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

            var attributes = new List<(CIAttribute attribute, long layerID)>();

            foreach (var layerID in layers)
            {
                CIAttribute? a;
                if (fullBinary)
                    a = await baseModel.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
                else
                    a = await baseModel.GetAttribute(name, ciid, layerID, trans, atTime);
                if (a != null)
                    attributes.Add((a, layerID));
            }

            var mergedAttributes = MergeAttributes(attributes, layers);

            if (mergedAttributes.Count() > 1)
                throw new Exception("Should never happen!");

            var ma = mergedAttributes.FirstOrDefault();
            // if the attribute is removed, we don't return it
            if (ma.Attribute.State == AttributeState.Removed)
                return null;
            return ma;
        }

        public async Task<IImmutableDictionary<string, MergedCIAttribute>> GetMergedAttributes(Guid ciid, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var d = await GetMergedAttributes(SpecificCIIDsSelection.Build(ciid), layers, trans, atTime);
            return d.GetValueOrDefault(ciid, ImmutableDictionary<string, MergedCIAttribute>.Empty);
        }

        public async Task<IImmutableDictionary<Guid, IImmutableDictionary<string, MergedCIAttribute>>> GetMergedAttributes(ICIIDSelection cs, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var ret = new Dictionary<Guid, IImmutableDictionary<string, MergedCIAttribute>>();

            if (layers.IsEmpty)
                return ImmutableDictionary<Guid, IImmutableDictionary<string, MergedCIAttribute>>.Empty; // return empty, an empty layer list can never produce any attributes

            var attributes = new List<(CIAttribute attribute, long layerID)>();

            foreach (var layerID in layers)
            {
                var la = await baseModel.GetAttributes(cs, layerID, trans, atTime);
                foreach (var a in la)
                    attributes.Add((a, layerID));
            }

            var mergedAttributes = MergeAttributes(attributes, layers);

            foreach (var ma in mergedAttributes)
            {
                var CIID = ma.Attribute.CIID;
                if (!ret.ContainsKey(CIID))
                    ret.Add(CIID, ImmutableDictionary<string, MergedCIAttribute>.Empty);
                ret[CIID] = ret[CIID].Add(ma.Attribute.Name, ma);
            }

            return ret.ToImmutableDictionary();
        }

        public async Task<IEnumerable<MergedCIAttribute>> FindMergedAttributesByName(string regex, ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            if (layers.IsEmpty)
                return ImmutableList<MergedCIAttribute>.Empty; // return empty, an empty layer list can never produce any attributes

            var attributes = new List<(CIAttribute attribute, long layerID)>();

            foreach (var layerID in layers)
            {
                var la = await baseModel.FindAttributesByName(regex, selection, layerID, trans, atTime);
                foreach (var a in la)
                    attributes.Add((a, layerID));
            }

            var mergedAttributes = MergeAttributes(attributes, layers);

            return mergedAttributes;
        }

        public async Task<IImmutableDictionary<Guid, MergedCIAttribute>> FindMergedAttributesByFullName(string name, ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var ret = new Dictionary<Guid, MergedCIAttribute>();

            if (layers.IsEmpty)
                return ImmutableDictionary<Guid, MergedCIAttribute>.Empty; // return empty, an empty layer list can never produce any attributes

            var attributes = new List<(CIAttribute attribute, long layerID)>();

            foreach (var layerID in layers)
            {
                var la = await baseModel.FindAttributesByFullName(name, selection, layerID, trans, atTime);
                foreach (var a in la)
                    attributes.Add((a, layerID));
            }

            var mergedAttributes = MergeAttributes(attributes, layers);

            foreach (var ma in mergedAttributes)
            {
                var CIID = ma.Attribute.CIID;
                ret.Add(CIID, ma);
            }

            return ret.ToImmutableDictionary();
        }

        public async Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await baseModel.GetAttributes(selection, layerID, trans, atTime);
        }

        public async Task<CIAttribute?> GetAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await baseModel.GetAttribute(name, ciid, layerID, trans, atTime);
        }
        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await baseModel.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await baseModel.FindAttributesByName(regex, selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await baseModel.FindAttributesByFullName(name, selection, layerID, trans, atTime);
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, long layerID, IChangesetProxy changeset, IModelContext trans)
        {
            return await baseModel.InsertAttribute(name, value, ciid, layerID, changeset, trans);
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, long layerID, IChangesetProxy changeset, IModelContext trans)
        {
            return await baseModel.RemoveAttribute(name, ciid, layerID, changeset, trans);
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertCINameAttribute(string nameValue, Guid ciid, long layerID, IChangesetProxy changeset, IModelContext trans)
        {
            return await baseModel.InsertCINameAttribute(nameValue, ciid, layerID, changeset, trans);
        }

        public async Task<IEnumerable<(Guid ciid, string fullName, IAttributeValue value, AttributeState state)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, IModelContext trans)
        {
            return await baseModel.BulkReplaceAttributes(data, changeset, trans);
        }
    }
}
