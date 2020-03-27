using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ITemplateModel
    {
        Task UpdateErrorsOfLayer(long layerID, ICIModel ciModel, long changesetID, NpgsqlTransaction trans);
        Task UpdateErrorsOfCI(string ciid, long layerID, ICIModel ciModel, long changesetID, NpgsqlTransaction trans);
    }
}
