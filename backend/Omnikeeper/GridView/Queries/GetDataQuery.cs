using MediatR;
using Npgsql;
using Omnikeeper.GridView.Response;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Queries
{
    public class GetDataQuery
    {
        public class Query : IRequest<GetDataResponse>
        {

        }

        public class GetDataQueryHandler : IRequestHandler<Query, GetDataResponse>
        {
            private readonly NpgsqlConnection conn;
            public GetDataQueryHandler(NpgsqlConnection connection)
            {
                conn = connection;
            }
            public async Task<GetDataResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var result = new GetDataResponse
                {
                    Rows = new List<Row>()
                };

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

                    var el = result.Rows.Find(el => el.Ciid == id);

                    if (el != null)
                    {
                        el.Cells.Add(new Cell
                        {
                            Name = name,
                            Value = value,
                            Changeable = true
                        });
                    }
                    else
                    {
                        result.Rows.Add(new Row
                        {
                            Ciid = id,
                            Cells = new List<Cell>
                                    {
                                        new Cell 
                                        {
                                            Name = name,
                                            Value = value,
                                            Changeable = true
                                        }
                                    }
                        });
                    }
                }

                return result;
            }
        }
    }
}
