using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IAttributeModel
    {
        Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetMergedAttributes(ICIIDSelection cs, IAttributeSelection attributeSelection, LayerSet layers, IModelContext trans, TimeThreshold atTime);

        Task<MergedCIAttribute?> GetFullBinaryMergedAttribute(string name, Guid ciid, LayerSet layerset, IModelContext trans, TimeThreshold atTime);

        Task<bool> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans, IMaskHandlingForRemoval maskHandling);
    }

    public static class AttributeModelExtensions
    {
        /**
         * NOTE: this does not return an entry for CIs that do not have a name specified; in other words, it can return less than specified via the selection parameter
         */
        public static async Task<IDictionary<Guid, string>> GetMergedCINames(this IAttributeModel attributeModel, ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var a = await attributeModel.GetMergedAttributes(selection, NamedAttributesSelection.Build(ICIModel.NameAttribute), layers, trans, atTime);

            // NOTE: because attributeModel.GetMergedAttributes() only returns inner dictionaries for CIs that contain ANY attributes, we can safely access the attribute in the dictionary by []
            var ret = a.ToDictionary(t => t.Key, t => t.Value[ICIModel.NameAttribute].Attribute.Value.Value2String());

            return ret;
        }

        public static async Task<IDictionary<Guid, MergedCIAttribute>> FindMergedAttributesByFullName(this IAttributeModel attributeModel, string name, ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var a = await attributeModel.GetMergedAttributes(selection, NamedAttributesSelection.Build(name), layers, trans, atTime);

            // NOTE: because attributeModel.GetMergedAttributes() only returns inner dictionaries for CIs that contain ANY attributes, we can safely access the attribute in the dictionary by []
            return a.ToDictionary(t => t.Key, t => t.Value[name]);
        }

        public static async Task<bool> InsertAttribute(this IAttributeModel attributeModel, string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans)
        {
            var data = new BulkCIAttributeDataCIAndAttributeNameScope(layerID, new BulkCIAttributeDataCIAndAttributeNameScope.Fragment[]
            {
                new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(ciid, name, value)
            }, new HashSet<Guid>() { ciid }, new HashSet<string>() { name });
            var maskHandling = MaskHandlingForRemovalApplyNoMask.Instance;
            return await attributeModel.BulkReplaceAttributes(data, changeset, origin, trans, maskHandling);
        }

        public static async Task<bool> RemoveAttribute(this IAttributeModel attributeModel, string name, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans, IMaskHandlingForRemoval maskHandling)
        {
            var data = new BulkCIAttributeDataCIAndAttributeNameScope(layerID, new BulkCIAttributeDataCIAndAttributeNameScope.Fragment[] { }, new HashSet<Guid>() { ciid }, new HashSet<string>() { name });
            return await attributeModel.BulkReplaceAttributes(data, changeset, origin, trans, maskHandling);
        }


        public static async Task<bool> InsertCINameAttribute(this IAttributeModel attributeModel, string nameValue, Guid ciid, string layerID, IChangesetProxy changesetProxy, DataOriginV1 origin, IModelContext trans)
            => await attributeModel.InsertAttribute(ICIModel.NameAttribute, new AttributeScalarValueText(nameValue), ciid, layerID, changesetProxy, origin, trans);
    }
}
