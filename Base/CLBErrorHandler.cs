using Landscape.Base.Model;
using LandscapePrototype.Entity.AttributeValues;
using Npgsql;
using System;
using System.Collections.Generic;
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

        public CLBErrorHandler(NpgsqlTransaction trans, string clbName, long clbLayerID, long changesetID, ICIModel ciModel)
        {
            this.trans = trans;
            this.clbName = clbName;
            this.clbLayerID = clbLayerID;
            this.changesetID = changesetID;
            this.ciModel = ciModel;
        }

        public async Task RemoveOutdatedErrors()
        {
            // TODO: get all attributes starting with clbName that are NOT created through this errorHandler instance and remove them
        }

        public async Task LogError(string ciid, string name, string message)
        {
            await ciModel.InsertAttribute($"errors.clb.{clbName}.{name}", AttributeValueText.Build(message), clbLayerID, ciid, changesetID, trans);
        }
    }
}
