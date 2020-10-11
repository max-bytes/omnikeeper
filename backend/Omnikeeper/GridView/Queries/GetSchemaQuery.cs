using MediatR;
using Npgsql;
using Omnikeeper.GridView.Response;
using Omnikeeper.GridView.Service;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Queries
{
    public class GetSchemaQuery
    {
        public class Query : IRequest<GetSchemaResponse>
        {

        }
        public class GetSchemaQueryHandler : IRequestHandler<Query, GetSchemaResponse>
        {
            private readonly NpgsqlConnection conn;
            private readonly GridViewConfigService _gridViewConfigService;
            public GetSchemaQueryHandler(NpgsqlConnection connection, GridViewConfigService gridViewConfigService)
            {
                conn = connection;
                _gridViewConfigService = gridViewConfigService;
            }
            public async Task<GetSchemaResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var result = new GetSchemaResponse 
                {
                    ShowCIIDColumn = true,
                    Columns = new List<Column>()
                };

                var config = await _gridViewConfigService.GetConfiguration("test");

                // TO DO
                //var trans = null;

                using var command = new NpgsqlCommand($@"
                    SELECT CI.id, ATTR.name, ATTR.value
                    FROM attribute ATTR
                    INNER JOIN ci CI ON ATTR.ci_id = CI.id
                    ORDER BY CI.id
                ", conn, null);

                using var dr = await command.ExecuteReaderAsync();

                while (dr.Read())
                {
                    var id = dr.GetGuid(0);
                    var name = dr.GetString(1);
                    var value = dr.GetString(2);

                    result.Columns.Add(new Column
                    {
                        Name = name,
                        Description = ""
                    });
                }

                return result;
            }
        }
    }
}
