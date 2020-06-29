using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Landscape.Base.Inbound
{
    public interface IExternalIDMapPersister
    {
        public Task Persist(IDictionary<Guid, string> int2ext);
        public Task<IDictionary<Guid, string>> Load();
    }

    public class ExternalIDMapper
    {
        private IDictionary<Guid, string> int2ext;
        private IDictionary<string, Guid> ext2int;
        private IExternalIDMapPersister persister = null;

        private bool loaded = false;

        public ExternalIDMapper()
        {
            int2ext = new Dictionary<Guid, string>();
            ext2int = new Dictionary<string, Guid>();
        }

        public void SetPersister(IExternalIDMapPersister persister)
        {
            this.persister = persister;
        }

        public async Task Setup()
        {
            if (!loaded)
            {
                if (persister != null)
                {
                    var data = await persister.Load();
                    if (data != null)
                    {
                        int2ext = data;
                        ext2int = data.ToDictionary(x => x.Value, x => x.Key);
                    }
                }
                loaded = true;
            }
        }

        public void Add(Guid ciid, string externalID)
        {
            int2ext.Add(ciid, externalID);
            ext2int.Add(externalID, ciid);
        }

        public void RemoveViaExternalID(string externalID)
        {
            if (ext2int.TryGetValue(externalID, out var ciid))
            {
                ext2int.Remove(externalID);
                int2ext.Remove(ciid);
            }
        }

        public IEnumerable<string> RemoveAllExceptExternalIDs(IEnumerable<string> externalIDsToKeep)
        {
            var remove = ext2int.Keys.Except(externalIDsToKeep).ToList(); // the ToList() is important here, to ensure the Except() is evaluated right now, not later when the collections are modified
            foreach(var r in remove)
            {
                RemoveViaExternalID(r);
            }
            return remove;
        }

        public IEnumerable<(Guid, string)> GetIDPairs(ISet<Guid> fromSubSelectionCIIDs)
        {
            if (fromSubSelectionCIIDs != null)
                return int2ext.Where(kv => fromSubSelectionCIIDs.Contains(kv.Key)).Select(kv => (kv.Key, kv.Value));
            else
                return int2ext.Select(kv => (kv.Key, kv.Value));
        }

        public bool ExistsInternally(string externalID) => ext2int.ContainsKey(externalID);

        public IEnumerable<Guid> GetAllCIIDs() => int2ext.Keys;

        public Guid? GetCIID(string externalId)
        {
            ext2int.TryGetValue(externalId, out var ciid);
            return ciid;
        }

        public async Task Persist()
        {
            if (persister != null)
                await persister.Persist(int2ext);
        }
    }
}
