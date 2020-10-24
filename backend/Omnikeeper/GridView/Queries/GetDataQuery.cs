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
            public string Context { get; set; }
            public int? PageSize { get; set; }
            public int? PageIndex { get; set; }
        }

        public class GetDataQueryHandler : IRequestHandler<Query, GetDataResponse>
        {
            private readonly GridViewConfigService gridViewConfigService;
            private readonly IEffectiveTraitModel effectiveTraitModel;

            public GetDataQueryHandler(GridViewConfigService gridViewConfigService, IEffectiveTraitModel effectiveTraitModel)
            {
                this.gridViewConfigService = gridViewConfigService;
                this.effectiveTraitModel = effectiveTraitModel;
            }

            public async Task<GetDataResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                // TO DO: implement pagination

                var pageSize = request.PageSize ?? 10;
                var pageIndex = request.PageIndex ?? 0;

                var config = await gridViewConfigService.GetConfiguration(request.Context);


                var result = new GetDataResponse
                {
                    Rows = new List<Row>()
                };

                var attributes = new List<CIAttribute>();

                // TO DO transaction parameter should not be null

                var res = await effectiveTraitModel.CalculateEffectiveTraitsForTraitName(
                    config.Trait,
                    new LayerSet(config.ReadLayerset.ToArray()),
                    null,
                    TimeThreshold.BuildLatest()
                    );

                foreach (var item in res)
                {
                    var ci_id = item.Key;

                    foreach (var attr in item.Value.TraitAttributes)
                    {
                        var c = attr.Value;
                        var name = attr.Value.Attribute.Name;

                        var col = config.Columns.Find(el => el.SourceAttributeName == name);

                        if (col == null)
                        {
                            continue;
                        }

                        var el = result.Rows.Find(el => el.Ciid == ci_id);

                        if (el != null)
                        {
                            el.Cells.Add(new Cell
                            {
                                Name = name,
                                Value = attr.Value.Attribute.Value.Value2String(),
                                Changeable = col.WriteLayer != null
                            });
                        }
                        else
                        {
                            result.Rows.Add(new Row
                            {
                                Ciid = ci_id,
                                Cells = new List<Cell>
                                    {
                                        new Cell
                                        {
                                            Name = name,
                                            Value = attr.Value.Attribute.Value.Value2String(),
                                            Changeable = col.WriteLayer != null
                                        }
                                    }
                            });
                        }
                    }
                }

                return result;
            }
        }
    }
}
