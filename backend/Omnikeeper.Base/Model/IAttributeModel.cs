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
        Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetMergedAttributes(ICIIDSelection cs, IAttributeSelection attributeSelection, LayerSet layers, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling);

        Task<MergedCIAttribute?> GetFullBinaryMergedAttribute(string name, Guid ciid, LayerSet layerset, IModelContext trans, TimeThreshold atTime);

        Task<int> BulkReplaceAttributes<F>(IBulkCIAttributeData<F> data, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans,
            IMaskHandlingForRemoval maskHandling, IOtherLayersValueHandling otherLayersValueHandling);
    }

    public static class AttributeModelExtensions
    {
        /**
         * NOTE: this does not return an entry for CIs that do not have a name specified; in other words, it can return less than specified via the selection parameter
         */
        public static async Task<IDictionary<Guid, string>> GetMergedCINames(this IAttributeModel attributeModel, ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var a = await attributeModel.GetMergedAttributes(selection, NamedAttributesSelection.Build(ICIModel.NameAttribute), layers, trans, atTime, GeneratedDataHandlingInclude.Instance);

            // NOTE: because attributeModel.GetMergedAttributes() only returns inner dictionaries for CIs that contain ANY attributes, we can safely access the attribute in the dictionary by []
            var ret = a.ToDictionary(t => t.Key, t => t.Value[ICIModel.NameAttribute].Attribute.Value.Value2String());

            return ret;
        }

        public static async Task<IDictionary<Guid, MergedCIAttribute>> FindMergedAttributesByFullName(this IAttributeModel attributeModel, string name, ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var a = await attributeModel.GetMergedAttributes(selection, NamedAttributesSelection.Build(name), layers, trans, atTime, GeneratedDataHandlingInclude.Instance);

            // NOTE: because attributeModel.GetMergedAttributes() only returns inner dictionaries for CIs that contain ANY attributes, we can safely access the attribute in the dictionary by []
            return a.ToDictionary(t => t.Key, t => t.Value[name]);
        }

        public static async Task<bool> InsertAttribute(this IAttributeModel attributeModel, string name, IAttributeValue value, Guid ciid, string layerID, IChangesetProxy changeset,
            DataOriginV1 origin, IModelContext trans, IOtherLayersValueHandling otherLayersValueHandling)
        {
            var data = new BulkCIAttributeDataCIAndAttributeNameScope(layerID, new BulkCIAttributeDataCIAndAttributeNameScope.Fragment[]
            {
                new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(ciid, name, value)
            }, new HashSet<Guid>() { ciid }, new HashSet<string>() { name });
            var maskHandling = MaskHandlingForRemovalApplyNoMask.Instance; // NOTE: we can keep this fixed here, because it does not affect inserts
            var r = await attributeModel.BulkReplaceAttributes(data, changeset, origin, trans, maskHandling, otherLayersValueHandling);
            return r > 0;
        }

        public static async Task<bool> RemoveAttribute(this IAttributeModel attributeModel, string name, Guid ciid, string layerID, IChangesetProxy changeset, DataOriginV1 origin, IModelContext trans, IMaskHandlingForRemoval maskHandling)
        {
            var data = new BulkCIAttributeDataCIAndAttributeNameScope(layerID, new BulkCIAttributeDataCIAndAttributeNameScope.Fragment[] { }, new HashSet<Guid>() { ciid }, new HashSet<string>() { name });
            var otherLayersValueHandling = OtherLayersValueHandlingForceWrite.Instance; // NOTE: we can keep this fixed here, because it does not affect removals
            var r = await attributeModel.BulkReplaceAttributes(data, changeset, origin, trans, maskHandling, otherLayersValueHandling);
            return r > 0;
        }


        public static async Task<bool> InsertCINameAttribute(this IAttributeModel attributeModel, string nameValue, Guid ciid, string layerID, IChangesetProxy changesetProxy,
            DataOriginV1 origin, IModelContext trans, IOtherLayersValueHandling otherLayersValueHandling)
            => await attributeModel.InsertAttribute(ICIModel.NameAttribute, new AttributeScalarValueText(nameValue), ciid, layerID, changesetProxy, origin, trans, otherLayersValueHandling);
    }
}
