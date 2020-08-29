using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Utils;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
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

            return compound.Select(t => MergedCIAttribute.Build(t.Value.First().Value.attribute, layerStackIDs: t.Value.Select(tt => tt.Value.layerID).Reverse().ToArray()));
        }

        public async Task<MergedCIAttribute> GetMergedAttribute(Guid ciid, string name, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (layers.IsEmpty)
                return null; // return empty, an empty layer list can never produce any attributes

            var attributes = new List<(CIAttribute attribute, long layerID)>();

            foreach (var layerID in layers)
            {
                var a = await baseModel.GetAttribute(name, layerID, ciid, trans, atTime);
                if (a != null)
                    attributes.Add((a, layerID));
            }

            var mergedAttributes = MergeAttributes(attributes, layers);

            if (mergedAttributes.Count() > 1)
                throw new Exception("Should never happen!");

            return mergedAttributes.FirstOrDefault();
        }

        public async Task<IImmutableDictionary<string, MergedCIAttribute>> GetMergedAttributes(Guid ciid, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var d = await GetMergedAttributes(SpecificCIIDsSelection.Build(ciid), layers, trans, atTime);
            return d.GetValueOrDefault(ciid, ImmutableDictionary<string, MergedCIAttribute>.Empty);
        }

        public async Task<IImmutableDictionary<Guid, IImmutableDictionary<string, MergedCIAttribute>>> GetMergedAttributes(ICIIDSelection cs, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
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

        public async Task<IEnumerable<MergedCIAttribute>> FindMergedAttributesByName(string regex, ICIIDSelection selection, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
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

        public async Task<IImmutableDictionary<Guid, MergedCIAttribute>> FindMergedAttributesByFullName(string name, ICIIDSelection selection, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime)
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

        public async Task<IEnumerable<CIAttribute>> GetAttributes(ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await baseModel.GetAttributes(selection, layerID, trans, atTime);
        }

        public async Task<CIAttribute> GetAttribute(string name, long layerID, Guid ciid, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await baseModel.GetAttribute(name, layerID, ciid, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByName(string regex, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await baseModel.FindAttributesByName(regex, selection, layerID, trans, atTime);
        }

        public async Task<IEnumerable<CIAttribute>> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            return await baseModel.FindAttributesByFullName(name, selection, layerID, trans, atTime);
        }

        public async Task<CIAttribute> InsertAttribute(string name, IAttributeValue value, long layerID, Guid ciid, IChangesetProxy changeset, NpgsqlTransaction trans)
        {
            return await baseModel.InsertAttribute(name, value, layerID, ciid, changeset, trans);
        }

        public async Task<CIAttribute> RemoveAttribute(string name, long layerID, Guid ciid, IChangesetProxy changeset, NpgsqlTransaction trans)
        {
            return await baseModel.RemoveAttribute(name, layerID, ciid, changeset, trans);
        }

        public async Task<CIAttribute> InsertCINameAttribute(string nameValue, long layerID, Guid ciid, IChangesetProxy changeset, NpgsqlTransaction trans)
        {
            return await baseModel.InsertCINameAttribute(nameValue, layerID, ciid, changeset, trans);
        }

        public async Task<bool> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, NpgsqlTransaction trans)
        {
            return await baseModel.BulkReplaceAttributes(data, changeset, trans);
        }
    }
}
