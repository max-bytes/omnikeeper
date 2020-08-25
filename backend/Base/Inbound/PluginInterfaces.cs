using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using static Landscape.Base.Model.IRelationModel;

namespace Landscape.Base.Inbound
{
    public interface IExternalIDMapPersister
    {
        public Task Persist(string scope, IDictionary<Guid, string> int2ext);
        public Task<IDictionary<Guid, string>> Load(string scope);
    }

    public interface IExternalID
    {
        string ConvertToString();
    }

    public struct ExternalIDString : IExternalID, IEquatable<ExternalIDString>
    {
        public string ID { get; }

        public ExternalIDString(string id)
        {
            ID = id;
        }

        public string ConvertToString() => ID;
        public override string ToString()
        {
            return ID;
        }

        public override bool Equals([AllowNull] object other) => Equals(other as ExternalIDString?);
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

        public string ConvertToString() => ID.ToString();
        public override string ToString()
        {
            return ID.ToString();
        }

        public override bool Equals([AllowNull] object other) => Equals(other as ExternalIDGuid?);
        public bool Equals([AllowNull] ExternalIDGuid other) => ID == other.ID;
        public override int GetHashCode() => ID.GetHashCode();
    }

    public interface IExternalItem<EID> where EID : IExternalID
    {
        public EID ID { get; }
    }

    public interface IExternalIDManager
    {
        Task<bool> Update(ICIModel ciModel, IAttributeModel attributeModel, NpgsqlTransaction trans, ILogger logger);
        TimeSpan PreferredUpdateRate { get; }
    }

    public interface IOnlineInboundLayerAccessProxy
    {
        string Name { get; }

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
        public IOnlineInboundAdapter Build(IOnlineInboundAdapter.IConfig config, IConfiguration appConfig, IExternalIDMapper externalIDMapper, IExternalIDMapPersister persister);
    }

    public interface IOnlineInboundAdapter
    {
        public interface IConfig
        {
            public string BuilderName { get; }
        }

        IExternalIDManager GetExternalIDManager();

        IOnlineInboundLayerAccessProxy GetLayerAccessProxy(Layer layer);
    }
}
