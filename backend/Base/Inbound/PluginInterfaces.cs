using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
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
        IAsyncEnumerable<CIAttribute> GetAttributes(ISet<Guid> ciids, TimeThreshold atTime);
        IAsyncEnumerable<CIAttribute> GetAttributesWithName(string name, TimeThreshold atTime);
        IAsyncEnumerable<Relation> GetRelations(Guid? ciid, IncludeRelationDirections ird, TimeThreshold atTime);
        IAsyncEnumerable<Relation> GetRelationsWithPredicateID(string predicateID, TimeThreshold atTime);
    }

    public interface IOnlineAccessProxy
    {
        IAsyncEnumerable<(CIAttribute attribute, long layerID)> GetAttributes(ISet<Guid> ciids, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
        IAsyncEnumerable<(CIAttribute attribute, long layerID)> GetAttributesWithName(string name, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);

        IAsyncEnumerable<(Relation relation, long layerID)> GetRelations(Guid? ciid, LayerSet layerset, IncludeRelationDirections ird, NpgsqlTransaction trans, TimeThreshold atTime);
        IAsyncEnumerable<(Relation relation, long layerID)> GetRelationsWithPredicateID(string predicateID, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime);
    }

    public interface IOnlineInboundLayerPluginBuilder
    {
        public string Name { get; }
        public IOnlineInboundLayerPlugin Build(IOnlineInboundLayerPlugin.IConfig config);
    }

    public interface IOnlineInboundLayerPlugin
    {
        public interface IConfig
        {

        }

        IExternalIDManager GetExternalIDManager(ICIModel ciModel);

        IOnlineInboundLayerAccessProxy GetLayerAccessProxy(Layer layer);
    }
}
