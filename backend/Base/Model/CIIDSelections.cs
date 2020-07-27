using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Landscape.Base.Model
{
    public interface ICIIDSelection
    {
        string WhereClause { get; }
        void AddParameters(NpgsqlParameterCollection p);
    }

    public class SingleCIIDSelection : ICIIDSelection
    {
        public Guid CIID { get; }
        public SingleCIIDSelection(Guid ciid)
        {
            CIID = ciid;
        }
        public string WhereClause => "ci_id = @ci_id";
        public void AddParameters(NpgsqlParameterCollection p) => p.AddWithValue("ci_id", CIID);
    }

    public class MultiCIIDsSelection : ICIIDSelection
    {
        public Guid[] CIIDs { get; }
        public MultiCIIDsSelection(Guid[] ciids)
        {
            CIIDs = ciids;
        }
        public MultiCIIDsSelection(IEnumerable<Guid> ciids)
        {
            CIIDs = ciids.ToArray();
        }
        public string WhereClause => "ci_id = ANY(@ci_ids)";
        public void AddParameters(NpgsqlParameterCollection p) => p.AddWithValue("ci_ids", CIIDs);
    }

    public class AllCIIDsSelection : ICIIDSelection
    {
        public string WhereClause => "1=1";
        public void AddParameters(NpgsqlParameterCollection p) { }
    }
}
