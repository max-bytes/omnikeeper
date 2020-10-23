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
            public string Context { get; set; }
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

                var config = await _gridViewConfigService.GetConfiguration(request.Context);

                var result = new GetSchemaResponse 
                {
                    ShowCIIDColumn = config.ShowCIIDColumn,
                    Columns = new List<Column>()
                };

                // TO DO
                //var trans = null;

                config.Columns.ForEach(el => result.Columns.Add(new Column
                {
                    Name = el.SourceAttributeName,
                    Description = el.ColumnDescription
                }));

                return result;
            }
        }
    }
}
