using Landscape.Base.Entity;
using Landscape.Base.Model;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Landscape.Base.Inbound
{
    public interface IExternalItem
    {
        public string ID { get; }
    }

    public interface IExternalIDManager
    {
        Task Update(NpgsqlConnection conn, ILogger logger);
    }

    public interface IOnlineInboundLayerAccessProxy
    {
        IAsyncEnumerable<CIAttribute> GetAttributes(ISet<Guid> ciids);
        IAsyncEnumerable<CIAttribute> GetAttributesWithName(string name);
    }

    public interface IOnlineAccessProxy
    {
        IAsyncEnumerable<(CIAttribute attribute, long layerID)> GetAttributes(ISet<Guid> ciids, LayerSet layers, NpgsqlTransaction trans);
        IAsyncEnumerable<(CIAttribute attribute, long layerID)> GetAttributesWithName(string name, LayerSet layers, NpgsqlTransaction trans);
    }

    public interface IOnlineInboundLayerPlugin
    {
        IExternalIDManager GetExternalIDManager(ICIModel ciModel);

        IOnlineInboundLayerAccessProxy GetLayerAccessProxy();

        string Name { get; }
    }
}
