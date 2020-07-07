using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Landscape.Base.Inbound
{
    public interface IExternalIDMapper
    {
        S RegisterScoped<S>(S scoped) where S : class, IScopedExternalIDMapper;
    }
    public interface IScopedExternalIDMapper
    {
        Task Setup();
        string Scope { get; }
    }

    public class ExternalIDMapper : IExternalIDMapper
    {
        private readonly IDictionary<string, IScopedExternalIDMapper> scopes = new Dictionary<string, IScopedExternalIDMapper>();

        public ExternalIDMapper()
        {
        }

        public S RegisterScoped<S>(S scoped) where S : class, IScopedExternalIDMapper
        {
            if (scopes.TryGetValue(scoped.Scope, out var existingScoped)) return existingScoped as S;
            scopes.Add(scoped.Scope, scoped);
            return scoped;
        }
    }

    public abstract class ScopedExternalIDMapper<EID> : IScopedExternalIDMapper where EID : IExternalID
    {
        private IDictionary<Guid, EID> int2ext;
        private IDictionary<EID, Guid> ext2int;
        public string Scope { get; }
        private readonly Func<string, EID> string2ExtIDF;
        private readonly IExternalIDMapPersister persister;

        private bool loaded = false;

        public ScopedExternalIDMapper(string scope, IExternalIDMapPersister persister, Func<string, EID> string2ExtIDF)
        {
            int2ext = new ConcurrentDictionary<Guid, EID>();
            ext2int = new ConcurrentDictionary<EID, Guid>();
            Scope = scope;
            this.string2ExtIDF = string2ExtIDF;
            this.persister = persister;
        }

        public abstract Guid? DeriveCIIDFromExternalID(EID externalID);

        public async Task Setup()
        {
            if (!loaded)
            {
                var data = await persister.Load(Scope);
                if (data != null)
                {
                    int2ext = data.ToDictionary(kv => kv.Key, kv => string2ExtIDF(kv.Value));
                    ext2int = int2ext.ToDictionary(x => x.Value, x => x.Key);
                }
                loaded = true;
            }
        }

        public void Add(Guid ciid, EID externalID)
        {
            int2ext.Add(ciid, externalID);
            ext2int.Add(externalID, ciid);
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
            foreach(var r in remove)
            {
                RemoveViaExternalID(r);
            }
            return remove;
        }

        public IEnumerable<(Guid, EID)> GetIDPairs(ISet<Guid> fromSubSelectionCIIDs)
        {
            if (fromSubSelectionCIIDs != null)
                return int2ext.Where(kv => fromSubSelectionCIIDs.Contains(kv.Key)).Select(kv => (kv.Key, kv.Value));
            else
                return int2ext.Select(kv => (kv.Key, kv.Value));
        }

        public bool ExistsInternally(EID externalID) => ext2int.ContainsKey(externalID);

        public IEnumerable<Guid> GetAllCIIDs() => int2ext.Keys;

        public Guid? GetCIID(EID externalId)
        {
            ext2int.TryGetValue(externalId, out var ciid);
            return ciid;
        }

        public async Task Persist()
        {
            await persister.Persist(Scope, int2ext.ToDictionary(kv => kv.Key, kv => kv.Value.ConvertToString()));
        }
    }
}
