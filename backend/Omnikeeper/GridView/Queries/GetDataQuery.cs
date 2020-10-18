using MediatR;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
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
            public int? PageSize { get; set; }
            public int? PageIndex { get; set; }
        }

        public class GetDataQueryHandler : IRequestHandler<Query, GetDataResponse>
        {
            private readonly GridViewConfigService gridViewConfigService;
            private readonly IAttributeModel attributeModel;
            private readonly ICIModel ciModel;

            public GetDataQueryHandler(GridViewConfigService gridViewConfigService, IAttributeModel attributeModel, ICIModel ciModel)
            {
                this.gridViewConfigService = gridViewConfigService;
                this.attributeModel = attributeModel;
                this.ciModel = ciModel;
            }

            public async Task<GetDataResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                // TO DO: implement pagination

                var pageSize = request.PageSize ?? 10;
                var pageIndex = request.PageIndex ?? 0;

                var config = await gridViewConfigService.GetConfiguration(request.ConfigurationName);


                var result = new GetDataResponse
                {
                    Rows = new List<Row>()
                };


                // TO DO
                // 1. Filter using a traitset
                // 2. Only CIs that fulfill/ have ALL of the traits in the Traitset are shown in the GridView

                var ciIds = await ciModel.GetCIIDs(null);

                var attributes = new List<CIAttribute>();

                // TO DO transaction parameter should not be null

                foreach (var layerId in config.ReadLayerset)
                {
                    var attrs = await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciIds), layerId, null, TimeThreshold.BuildLatest());
                    attributes.AddRange(attrs);
                }

                foreach (var attribute in attributes)
                {

                    if (!config.Columns.Any(el => el.SourceAttributeName == attribute.Name))
                    {
                        continue;
                    }

                    var el = result.Rows.Find(el => el.Ciid == attribute.CIID);

                    if (el != null)
                    {
                        el.Cells.Add(new Cell
                        {
                            Name = attribute.Name,
                            Value = attribute.Value.ToString(),
                            Changeable = true
                        });
                    }
                    else
                    {
                        result.Rows.Add(new Row
                        {
                            Ciid = attribute.CIID,
                            Cells = new List<Cell>
                                    {
                                        new Cell
                                        {
                                            Name = attribute.Name,
                                            Value = attribute.Value.ToString(),
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
