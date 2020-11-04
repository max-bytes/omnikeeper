using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.CLB
{
    public class CLBErrorHandler
    {
        private readonly NpgsqlTransaction trans;
        private readonly string clbName;
        private readonly long clbLayerID;
        private readonly IChangesetProxy changeset;
        private readonly IBaseAttributeModel attributeModel;

        private readonly IList<CIAttribute> writtenErrors = new List<CIAttribute>();

        public CLBErrorHandler(NpgsqlTransaction trans, string clbName, long clbLayerID, IChangesetProxy changeset, IBaseAttributeModel attributeModel)
        {
            this.trans = trans;
            this.clbName = clbName;
            this.clbLayerID = clbLayerID;
            this.changeset = changeset;
            this.attributeModel = attributeModel;
        }

        private string AttributeNamePrefix => $"__error.clb.{clbName}";

        // gets all attributes starting with AttributeNamePrefix that are NOT created through this errorHandler instance and remove them
        // TODO: rewrite into using bulk replace?
        public async Task RemoveOutdatedErrors()
        {
            var allAttributes = await attributeModel.FindAttributesByName($"^{AttributeNamePrefix}", new AllCIIDsSelection(), clbLayerID, trans, TimeThreshold.BuildLatest());

            var attributesToRemove = allAttributes.Where(a =>
            {
                return !writtenErrors.Any(we => we.ID == a.ID);
            });

            foreach (var remove in attributesToRemove)
            {
                await attributeModel.RemoveAttribute(remove.Name, remove.CIID, clbLayerID, changeset, trans);
            }
        }

        public async Task LogError(Guid ciid, string name, string message)
        {
            var a = await attributeModel.InsertAttribute($"{AttributeNamePrefix}.{name}", AttributeScalarValueText.BuildFromString(message, true), ciid, clbLayerID, changeset, trans);
            writtenErrors.Add(a.attribute);
        }
    }
}
