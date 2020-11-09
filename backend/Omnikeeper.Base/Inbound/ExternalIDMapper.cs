using Npgsql;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Inbound
{
    public interface IExternalIDMapper
    {
        Task<S> CreateOrGetScoped<S>(string scope, Func<S> scopedF, NpgsqlConnection conn, NpgsqlTransaction trans) where S : class, IScopedExternalIDMapper;
    }
    public interface IScopedExternalIDMapper
    {
        Task Setup(NpgsqlConnection conn, NpgsqlTransaction trans);

        string PersisterScope { get; }
    }

    /// <summary>
    /// singleton
    /// </summary>
    public class ExternalIDMapper : IExternalIDMapper
    {
        private readonly IDictionary<string, IScopedExternalIDMapper> scopes = new ConcurrentDictionary<string, IScopedExternalIDMapper>();

        public async Task<S> CreateOrGetScoped<S>(string scope, Func<S> scopedF, NpgsqlConnection conn, NpgsqlTransaction trans) where S : class, IScopedExternalIDMapper
        {
            if (scopes.TryGetValue(scope, out var existingScoped))
                return existingScoped as S;
            var scoped = scopedF();
            await scoped.Setup(conn, trans);
            scopes.Add(scope, scoped);
            return scoped;
        }
    }

    /// <summary>
    /// scoped
    /// </summary>
    /// <typeparam name="EID"></typeparam>
    public abstract class ScopedExternalIDMapper<EID> : IScopedExternalIDMapper where EID : struct, IExternalID
    {
        private IDictionary<Guid, EID> int2ext;
        private IDictionary<EID, Guid> ext2int;
        private readonly Func<string, EID> string2ExtIDF;
        private readonly IScopedExternalIDMapPersister persister;
        private bool loaded = false;

        public ScopedExternalIDMapper(IScopedExternalIDMapPersister persister, Func<string, EID> string2ExtIDF)
        {
            int2ext = new ConcurrentDictionary<Guid, EID>();
            ext2int = new ConcurrentDictionary<EID, Guid>();
            this.string2ExtIDF = string2ExtIDF;
            this.persister = persister;
        }

        public async Task Setup(NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            if (!loaded)
            {
                var data = await persister.Load(conn, trans);
                if (data == null)
                {
                    throw new Exception($"Failed to load persisted external IDs for scope {persister.Scope}");
                }

                // TODO: ensure that int2ext and ext2int both only contain unique keys AND are "equal"
                int2ext = data.ToDictionary(kv => kv.Key, kv => string2ExtIDF(kv.Value));
                ext2int = int2ext.GroupBy(x => x.Value).ToDictionary(x => x.Key, x => x.First().Key);

                loaded = true;
            }
        }

        public void Add(Guid ciid, EID externalID)
        {
            try
            {
                int2ext.Add(ciid, externalID);
                ext2int.Add(externalID, ciid);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void RemoveViaExternalID(EID externalID)
        {
            if (ext2int.TryGetValue(externalID, out var ciid))
            {
                ext2int.Remove(externalID);
                int2ext.Remove(ciid);
            }
        }

        public IEnumerable<EID> RemoveAllExceptExternalIDs(IEnumerable<EID> externalIDsToKeep)
        {
            var remove = ext2int.Keys.Except(externalIDsToKeep).ToList(); // the ToList() is important here, to ensure the Except() is evaluated right now, not later when the collections are modified
            foreach (var r in remove)
            {
                RemoveViaExternalID(r);
            }
            return remove;
        }

        public IEnumerable<(Guid ciid, EID externalID)> GetIDPairs(ISet<Guid> fromSubSelectionCIIDs)
        {
            if (fromSubSelectionCIIDs != null)
                return int2ext.Where(kv => fromSubSelectionCIIDs.Contains(kv.Key)).Select(kv => (kv.Key, kv.Value));
            else
                return int2ext.Select(kv => (kv.Key, kv.Value));
        }

        public bool ExistsInternally(EID externalID) => ext2int.ContainsKey(externalID);
        public bool ExistsExternally(Guid ciid) => int2ext.ContainsKey(ciid);

        public string PersisterScope => persister.Scope;

        public IEnumerable<Guid> GetAllCIIDs() => int2ext.Keys;

        public Guid? GetCIID(EID externalId)
        {
            ext2int.TryGetValue(externalId, out var ciid);
            return ciid == default ? null : new Guid?(ciid); // NOTE: guid is a value type, which is why we need to check for default and return null if so
        }
        public EID? GetExternalID(Guid ciid)
        {
            int2ext.TryGetValue(ciid, out var externalID);
            return externalID.Equals(default(EID)) ? null : new EID?(externalID); // NOTE: EID is a value type, which is why we need to check for default and return null if so
        }

        public async Task Persist(NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            await persister.Persist(int2ext.ToDictionary(kv => kv.Key, kv => kv.Value.SerializeToString()), conn, trans);
        }
    }
}
