using Omnikeeper.Base.Utils;
using SpanJson;
using System.Collections.Generic;

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

        [JsonCustomSerializer(typeof(GraphQLInputsFormatter))]
        public Dictionary<string, object>? Variables { get; set; }
    }
}
