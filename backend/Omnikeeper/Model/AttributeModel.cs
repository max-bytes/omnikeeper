using Npgsql;
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
        private IEnumerable<MergedCIAttribute> MergeAttributes(IEnumerable<(IEnumerable<CIAttribute> attributes, long layerID)> attributes)
        {
            var compound = new Dictionary<(Guid ciid, string name), List<(CIAttribute attribute, long layerID)>>();
            foreach(var g in attributes)
            {
                var layerID = g.layerID;
                foreach (var attribute in g.attributes)
                {
                    compound.AddOrUpdate((attribute.CIID, attribute.Name),
                        () => new List<(CIAttribute attribute, long layerID)>() { (attribute, layerID) },
                        (old) => { old.Add((attribute, layerID)); return old; });
                }
            }
            return compound.Select(t => new MergedCIAttribute(t.Value.First().attribute, layerStackIDs: t.Value.Select(tt => tt.layerID).Reverse().ToArray()));
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

            var attributes = new (IEnumerable<CIAttribute> attributes, long layerID)[layers.Length];
            var i = 0;
            foreach (var layerID in layers)
            {
                CIAttribute? a;
                if (fullBinary)
                    a = await baseModel.GetFullBinaryAttribute(name, ciid, layerID, trans, atTime);
                else
                    a = await baseModel.GetAttribute(name, ciid, layerID, trans, atTime);
                if (a != null)
                    attributes[i++] = (new CIAttribute[] { a }, layerID);
            }

            var mergedAttributes = MergeAttributes(attributes);

            if (mergedAttributes.Count() > 1)
                throw new Exception("Should never happen!");

            var ma = mergedAttributes.FirstOrDefault();
            // if the attribute is removed, we don't return it
            if (ma.Attribute.State == AttributeState.Removed)
                return null;
            return ma;
        }

        public async Task<IDictionary<string, MergedCIAttribute>> GetMergedAttributes(Guid ciid, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var d = await GetMergedAttributes(SpecificCIIDsSelection.Build(ciid), layers, trans, atTime);
            return d.GetValueOrDefault(ciid, new Dictionary<string, MergedCIAttribute>());
        }

        public async Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetMergedAttributes(ICIIDSelection cs, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var ret = new Dictionary<Guid, IDictionary<string, MergedCIAttribute>>();

            if (layers.IsEmpty)
                return ImmutableDictionary<Guid, IDictionary<string, MergedCIAttribute>>.Empty; // return empty, an empty layer list can never produce any attributes

            var attributes = new (IEnumerable<CIAttribute> attributes, long layerID)[layers.Length];
            var i = 0;
            foreach (var layerID in layers)
            {
                var la = await baseModel.GetAttributes(cs, layerID, trans, atTime);
                attributes[i++] = (la, layerID);
            }

            var mergedAttributes = MergeAttributes(attributes);

            foreach (var ma in mergedAttributes)
            {
                var CIID = ma.Attribute.CIID;
                if (!ret.ContainsKey(CIID))
                    ret.Add(CIID, new Dictionary<string, MergedCIAttribute>());
                ret[CIID].Add(ma.Attribute.Name, ma);
            }

            return ret;
        }

        public async Task<IEnumerable<MergedCIAttribute>> FindMergedAttributesByName(string regex, ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            if (layers.IsEmpty)
                return ImmutableList<MergedCIAttribute>.Empty; // return empty, an empty layer list can never produce any attributes

            var attributes = new (IEnumerable<CIAttribute> attributes, long layerID)[layers.Length];
            var i = 0;
            foreach (var layerID in layers)
            {
                var la = await baseModel.FindAttributesByName(regex, selection, layerID, trans, atTime);
                attributes[i++] = (la, layerID);
            }

            var mergedAttributes = MergeAttributes(attributes);

            return mergedAttributes;
        }

        public async Task<IDictionary<Guid, MergedCIAttribute>> FindMergedAttributesByFullName(string name, ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var ret = new Dictionary<Guid, MergedCIAttribute>();

            if (layers.IsEmpty)
                return ImmutableDictionary<Guid, MergedCIAttribute>.Empty; // return empty, an empty layer list can never produce any attributes

            var attributes = new (IEnumerable<CIAttribute> attributes, long layerID)[layers.Length];
            var i = 0;
            foreach (var layerID in layers)
            {
                var la = await baseModel.FindAttributesByFullName(name, selection, layerID, trans, atTime);
                attributes[i++] = (la, layerID);
            }

            var mergedAttributes = MergeAttributes(attributes);

            foreach (var ma in mergedAttributes)
            {
                var CIID = ma.Attribute.CIID;
                ret.Add(CIID, ma);
            }

            return ret;
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

        public async Task<IEnumerable<Guid>> FindCIIDsWithAttribute(string name, ICIIDSelection selection, long layerID, IModelContext trans, TimeThreshold atTime)
        {
            return await baseModel.FindCIIDsWithAttribute(name, selection, layerID, trans, atTime);
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertAttribute(string name, IAttributeValue value, Guid ciid, long layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            return await baseModel.InsertAttribute(name, value, ciid, layerID, changeset, origin, trans);
        }

        public async Task<(CIAttribute attribute, bool changed)> RemoveAttribute(string name, Guid ciid, long layerID, IChangesetProxy changeset, IModelContext trans)
        {
            return await baseModel.RemoveAttribute(name, ciid, layerID, changeset, trans);
        }

        public async Task<(CIAttribute attribute, bool changed)> InsertCINameAttribute(string nameValue, Guid ciid, long layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            return await baseModel.InsertCINameAttribute(nameValue, ciid, layerID, changeset, origin, trans);
        }

        public async Task<IEnumerable<(Guid ciid, string fullName, IAttributeValue value, AttributeState state)>> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            return await baseModel.BulkReplaceAttributes(data, changeset, origin, trans);
        }
    }
}
