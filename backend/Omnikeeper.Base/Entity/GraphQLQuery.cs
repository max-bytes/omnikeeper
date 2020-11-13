using Newtonsoft.Json.Linq;

namespace Omnikeeper.Base.Entity
{
    public class GraphQLQuery
    {
        //public GraphQLQuery(string operationName, string namedQuery, string query, JObject variables)
        //{
        //    OperationName = operationName;
        //    NamedQuery = namedQuery;
        //    Query = query;
        //    Variables = variables;
        //}

        public string? OperationName { get; set; }
        public string? NamedQuery { get; set; }
        public string? Query { get; set; }
        public JObject? Variables { get; set; }
    }
}
