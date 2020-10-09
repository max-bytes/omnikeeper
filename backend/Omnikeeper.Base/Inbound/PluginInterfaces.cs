using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Inbound
{
    public interface IExternalIDMapPersister
    {
        public Task Persist(string scope, IDictionary<Guid, string> int2ext, NpgsqlConnection conn, NpgsqlTransaction trans);
        public Task<IDictionary<Guid, string>> Load(string scope, NpgsqlConnection conn, NpgsqlTransaction trans);
        public IScopedExternalIDMapPersister CreateScopedPersister(string scope);
        public Task<int> DeleteUnusedScopes(ISet<string> usedScopes, NpgsqlConnection conn, NpgsqlTransaction trans);
        public Task<ISet<Guid>> GetAllMappedCIIDs(NpgsqlConnection conn, NpgsqlTransaction trans);
    }

    public interface IScopedExternalIDMapPersister // TODO: needed? or can be merged into IExternalIDMapPersister?
    {
        public Task Persist(IDictionary<Guid, string> int2ext, NpgsqlConnection conn, NpgsqlTransaction trans);
        public Task<IDictionary<Guid, string>> Load(NpgsqlConnection conn, NpgsqlTransaction trans);
        public string Scope { get; }
    }

    public interface IExternalID
    {
        string SerializeToString();
    }

    public struct ExternalIDString : IExternalID, IEquatable<ExternalIDString>
    {
        public string ID { get; }

        public ExternalIDString(string id)
        {
            ID = id;
        }

        public string SerializeToString() => ID;
        public override string ToString()
        {
            return ID;
        }

        public override bool Equals([AllowNull] object other)
        {
            try { return Equals((ExternalIDString)other); } catch (InvalidCastException) { return false; };
        }
        public bool Equals([AllowNull] ExternalIDString other) => ID == other.ID;
        public override int GetHashCode() => ID.GetHashCode();
    }

    public struct ExternalIDGuid : IExternalID, IEquatable<ExternalIDGuid>
    {
        public Guid ID { get; }

        public ExternalIDGuid(Guid id)
        {
            ID = id;
        }

        public string SerializeToString() => ID.ToString();
        public override string ToString()
        {
            return ID.ToString();
        }

        public override bool Equals([AllowNull] object other)
        {
            try { return Equals((ExternalIDGuid)other); } catch (InvalidCastException) { return false; };
        }
        public bool Equals([AllowNull] ExternalIDGuid other) => ID == other.ID;
        public override int GetHashCode() => ID.GetHashCode();
    }

    public interface IExternalItem<EID> where EID : IExternalID
    {
        public EID ID { get; }
    }

    public interface IExternalIDManager
    {
        Task<bool> Update(ICIModel ciModel, IAttributeModel attributeModel, CIMappingService ciMappingService, NpgsqlConnection conn, NpgsqlTransaction trans, ILogger logger);
        TimeSpan PreferredUpdateRate { get; }
        string PersisterScope { get; }
    }

    public interface ILayerAccessProxy
    {
        IAsyncEnumerable<CIAttribute> GetAttributes(ICIIDSelection selection, TimeThreshold atTime);
        //IAsyncEnumerable<CIAttribute> GetAttributesWithName(string name, TimeThreshold atTime);
        IAsyncEnumerable<CIAttribute> FindAttributesByName(string regex, ICIIDSelection selection, TimeThreshold atTime);
        IAsyncEnumerable<CIAttribute> FindAttributesByFullName(string name, ICIIDSelection selection, TimeThreshold atTime);
        Task<CIAttribute> GetAttribute(string name, Guid ciid, TimeThreshold atTime);
        IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, TimeThreshold atTime);
        Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, TimeThreshold atTime);
    }

    public interface IOnlineAccessProxy
    {
        Task<bool> IsOnlineInboundLayer(long layerID, NpgsqlTransaction trans);

        IAsyncEnumerable<(CIAttribute attribute, long layerID)> GetAttributes(ICIIDSelection selection, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
        IAsyncEnumerable<CIAttribute> GetAttributes(ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);
        //IAsyncEnumerable<(CIAttribute attribute, long layerID)> GetAttributesWithName(string name, LayerSet layers, NpgsqlTransaction trans, TimeThreshold atTime);
        IAsyncEnumerable<CIAttribute> FindAttributesByName(string regex, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);
        IAsyncEnumerable<CIAttribute> FindAttributesByFullName(string name, ICIIDSelection selection, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<CIAttribute> GetAttribute(string name, long layerID, Guid ciid, NpgsqlTransaction trans, TimeThreshold atTime);

        //IAsyncEnumerable<(Relation relation, long layerID)> GetRelations(IRelationSelection rl, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime);
        IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, long layerID, NpgsqlTransaction trans, TimeThreshold atTime);
    }

    public interface IOnlineInboundAdapterBuilder
    {
        public string Name { get; }
        public IScopedExternalIDMapper BuildIDMapper(IScopedExternalIDMapPersister persister);
        public IOnlineInboundAdapter Build(IOnlineInboundAdapter.IConfig config, IConfiguration appConfig, IScopedExternalIDMapper scopedExternalIDMapper, ILoggerFactory loggerFactory);
    }

    public interface IOnlineInboundAdapter
    {
        public interface IConfig
        {
            public string BuilderName { get; }
            public string MapperScope { get; }

            public static MyJSONSerializer<IConfig> Serializer = new MyJSONSerializer<IConfig>(new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        IExternalIDManager GetExternalIDManager();

        ILayerAccessProxy CreateLayerAccessProxy(Layer layer);
    }
}
