using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Inbound
{
    public interface IExternalIDMapPersister
    {
        public Task<bool> Persist(string scope, IDictionary<Guid, string> int2ext, IModelContext trans);
        public Task<IDictionary<Guid, string>?> Load(string scope, IModelContext trans);
        public IScopedExternalIDMapPersister CreateScopedPersister(string scope);
        public Task<int> DeleteUnusedScopes(ISet<string> usedScopes, IModelContext trans);
        public Task<ISet<Guid>> GetAllMappedCIIDs(IModelContext trans);
    }

    public interface IScopedExternalIDMapPersister // TODO: needed? or can be merged into IExternalIDMapPersister?
    {
        public Task<bool> Persist(IDictionary<Guid, string> int2ext, IModelContext trans);
        public Task<IDictionary<Guid, string>?> Load(IModelContext trans);
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

        public override bool Equals(object? other)
        {
            var tmp = (ExternalIDString?)other;
            if (tmp != null)
                return Equals((ExternalIDString)tmp);
            return false;
        }
        public bool Equals(ExternalIDString other) => ID == other.ID;
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

        public override bool Equals(object? other)
        {
            var tmp = (ExternalIDGuid?)other;
            if (tmp != null)
                return Equals((ExternalIDGuid)tmp);
            return false;
        }
        public bool Equals(ExternalIDGuid other) => ID == other.ID;
        public override int GetHashCode() => ID.GetHashCode();
    }

    public interface IExternalItem<EID> where EID : IExternalID
    {
        public EID ID { get; }
    }

    public interface IExternalIDManager
    {
        Task<(bool updated, bool successful)> Update(ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, CIMappingService ciMappingService, IModelContext trans, ILogger logger);
        TimeSpan PreferredUpdateRate { get; }
        string PersisterScope { get; }
    }

    public interface ILayerAccessProxy
    {
        IAsyncEnumerable<CIAttribute> GetAttributes(ICIIDSelection selection, TimeThreshold atTime, IAttributeSelection attributeSelection);
        Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, TimeThreshold atTime);
        IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, TimeThreshold atTime);
        Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, TimeThreshold atTime);
    }

    public interface IOnlineAccessProxy
    {
        Task<bool> IsOnlineInboundLayer(string layerID, IModelContext trans);
        Task<bool> ContainsOnlineInboundLayer(LayerSet layerset, IModelContext trans);

        Task<IEnumerable<CIAttribute>[]> GetAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime, IAttributeSelection attributeSelection);
        Task<CIAttribute?> GetFullBinaryAttribute(string name, string layerID, Guid ciid, IModelContext trans, TimeThreshold atTime);

        IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, string layerID, IModelContext trans, TimeThreshold atTime);
        Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IModelContext trans, TimeThreshold atTime);
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

            public static NewtonSoftJSONSerializer<IConfig> Serializer = new NewtonSoftJSONSerializer<IConfig>(new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        IExternalIDManager GetExternalIDManager();

        ILayerAccessProxy CreateLayerAccessProxy(Layer layer);
    }
}
