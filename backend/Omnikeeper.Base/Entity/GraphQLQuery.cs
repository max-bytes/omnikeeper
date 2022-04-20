using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Utils;
using SpanJson;
using System.Collections.Generic;

namespace Omnikeeper.Base.Entity
{
    public class GraphQLQuery
    {
        [FromQuery(Name = "operationName")]
        public string? OperationName { get; set; }

        [FromQuery(Name = "query")]
        public string? Query { get; set; }

        [JsonCustomSerializer(typeof(GraphQLInputsFormatter))]
        [FromQuery(Name = "variables")]
        public Dictionary<string, object?>? Variables { get; set; }
    }
}
