using FluentValidation;
using MediatR;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.GridView.Response;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Queries
{
    public class GetDataQuery
    {
        public class Query : IRequest<(GetDataResponse, bool, string)>
        {
            public string Context { get; set; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.Context).NotEmpty();
            }
        }

        public class GetDataQueryHandler : IRequestHandler<Query, (GetDataResponse, bool, string)>
        {
            private readonly IGridViewConfigModel gridViewConfigModel;
            private readonly IEffectiveTraitModel effectiveTraitModel;
            private readonly ITraitsProvider traitsProvider;

            public GetDataQueryHandler(IGridViewConfigModel gridViewConfigModel, IEffectiveTraitModel effectiveTraitModel,
                ITraitsProvider traitsProvider)
            {
                this.gridViewConfigModel = gridViewConfigModel;
                this.effectiveTraitModel = effectiveTraitModel;
                this.traitsProvider = traitsProvider;
            }

            public async Task<(GetDataResponse, bool, string)> Handle(Query request, CancellationToken cancellationToken)
            {
                var validator = new QueryValidator();
                validator.ValidateAndThrow(request);
 
                var config = await gridViewConfigModel.GetConfiguration(request.Context);
                
                var result = new GetDataResponse
                {
                    Rows = new List<Row>()
                };

                // TO DO: transaction parameter should not be null

                // NOTE mcsuk: use effectiveTraitModel.GetMergedCIsWithTrait() instead

                var activeTrait = await traitsProvider.GetActiveTrait(config.Trait, null, TimeThreshold.BuildLatest());

                if (activeTrait == null)
                {
                    return (new GetDataResponse(), false, $"Active trait {config.Trait} was not found!");
                }

                var res = await effectiveTraitModel.GetMergedCIsWithTrait(
                    activeTrait,
                    new LayerSet(config.ReadLayerset.ToArray()),
                    null,
                    TimeThreshold.BuildLatest()
                    );

                foreach (var item in res)
                {
                    var ci_id = item.ID;

                    foreach (var attr in item.MergedAttributes)
                    {
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

                return (result, true, "");
            }
        }
    }
}
