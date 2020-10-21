using MediatR;
using Npgsql;
using Omnikeeper.GridView.Response;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Queries
{
    public class GetContextsQuery
    {
        public class Query : IRequest<GetContextsResponse>
        {

        }

        public class GetContextsQueryHandler : IRequestHandler<Query, GetContextsResponse>
        {
            private readonly NpgsqlConnection conn;
            public GetContextsQueryHandler(NpgsqlConnection connection)
            {
                conn = connection;
            }

            async Task<GetContextsResponse> IRequestHandler<Query, GetContextsResponse>.Handle(Query request, CancellationToken cancellationToken)
            {
                var result = new GetContextsResponse
                {
                    Contexts = new List<Context>()
                };

                using var command = new NpgsqlCommand($@"
                    SELECT id, name 
                    FROM gridview_config
                ", conn, null);

                using var dr = await command.ExecuteReaderAsync();

                while (dr.Read())
                {
                    result.Contexts.Add(new Context
                    {
                        Id = dr.GetInt32(0),
                        Name = dr.GetString(1)
                    });
                }

                return result;
            }
        }
    }
}
