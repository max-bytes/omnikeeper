using Landscape.Base.Model;
using LandscapePrototype.Entity;
using LandscapePrototype.Entity.AttributeValues;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base
{
    public class CLBErrorHandler
    {
        private readonly NpgsqlTransaction trans;
        private readonly string clbName;
        private readonly long clbLayerID;
        private readonly long changesetID;
        private readonly ICIModel ciModel;

        private readonly IList<CIAttribute> writtenErrors = new List<CIAttribute>();

        public CLBErrorHandler(NpgsqlTransaction trans, string clbName, long clbLayerID, long changesetID, ICIModel ciModel)
        {
            this.trans = trans;
            this.clbName = clbName;
            this.clbLayerID = clbLayerID;
            this.changesetID = changesetID;
            this.ciModel = ciModel;
        }

        private string AttributeNamePrefix => $"__error.clb.{clbName}";

        // gets all attributes starting with AttributeNamePrefix that are NOT created through this errorHandler instance and remove them
        // TODO: rewrite into using bulk replace?
        public async Task RemoveOutdatedErrors()
        {
            var allAttributes = await ciModel.FindAttributesByName($"{AttributeNamePrefix}%", false, clbLayerID, trans, DateTimeOffset.Now);

            var attributesToRemove = allAttributes.Where(a =>
            {
                return !writtenErrors.Any(we => we.ID == a.ID);
            });

            foreach(var remove in attributesToRemove)
            {
                await ciModel.RemoveAttribute(remove.Name, clbLayerID, remove.CIID, changesetID, trans);
            }
        }

        public async Task LogError(string ciid, string name, string message)
        {
            var a = await ciModel.InsertAttribute($"{AttributeNamePrefix}.{name}", AttributeValueText.Build(message), clbLayerID, ciid, changesetID, trans);
            writtenErrors.Add(a);
        }
    }
}
