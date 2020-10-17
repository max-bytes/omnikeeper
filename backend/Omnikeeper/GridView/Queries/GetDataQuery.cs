using MediatR;
using Npgsql;
using Omnikeeper.Base.Model;
using Omnikeeper.GridView.Response;
using Omnikeeper.GridView.Service;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Queries
{
    public class GetDataQuery
    {
        public class Query : IRequest<GetDataResponse>
        {
            public string ConfigurationName { get; set; }
        }

        public class GetDataQueryHandler : IRequestHandler<Query, GetDataResponse>
        {
            private readonly NpgsqlConnection conn;
            private readonly GridViewConfigService _gridViewConfigService;
            private readonly IAttributeModel attributeModel;

            public GetDataQueryHandler(NpgsqlConnection connection, GridViewConfigService gridViewConfigService, IAttributeModel attributeModel)
            {
                conn = connection;
                _gridViewConfigService = gridViewConfigService;
                this.attributeModel = attributeModel;
            }

            public async Task<GetDataResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var config = await _gridViewConfigService.GetConfiguration(request.ConfigurationName);

                var result = new GetDataResponse
                {
                    Rows = new List<Row>()
                };


                // TO DO
                // 1. Filter using a traitset
                // 2. Only CIs that fulfill/ have ALL of the traits in the Traitset are shown in the GridView

                // TO DO: Call the model directly no need to fetch data from db in this case

                //var attributes = await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciid), layerID, trans, atTime);

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

                    if (!config.Columns.Any(el => el.SourceAttributeName == name))
                    {
                        continue;
                    }

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
