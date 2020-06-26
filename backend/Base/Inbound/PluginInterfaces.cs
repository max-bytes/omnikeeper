using Landscape.Base.Entity;
using Landscape.Base.Model;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Landscape.Base.Model.IRelationModel;

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
        IAsyncEnumerable<Relation> GetRelations(Guid? ciid, IncludeRelationDirections ird);
        IAsyncEnumerable<Relation> GetRelationsWithPredicateID(string predicateID);
    }

    public interface IOnlineAccessProxy
    {
        IAsyncEnumerable<(CIAttribute attribute, long layerID)> GetAttributes(ISet<Guid> ciids, LayerSet layers, NpgsqlTransaction trans);
        IAsyncEnumerable<(CIAttribute attribute, long layerID)> GetAttributesWithName(string name, LayerSet layers, NpgsqlTransaction trans);

        IAsyncEnumerable<(Relation relation, long layerID)> GetRelations(Guid? ciid, LayerSet layerset, IncludeRelationDirections ird, NpgsqlTransaction trans);
        IAsyncEnumerable<(Relation relation, long layerID)> GetRelationsWithPredicateID(string predicateID, LayerSet layerset, NpgsqlTransaction trans);
    }

    public interface IOnlineInboundLayerPlugin
    {
        IExternalIDManager GetExternalIDManager(ICIModel ciModel);

        IOnlineInboundLayerAccessProxy GetLayerAccessProxy();

        string Name { get; }
    }
}
