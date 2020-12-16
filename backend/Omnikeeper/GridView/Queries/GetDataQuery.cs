using FluentValidation;
using MediatR;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Model;
using Omnikeeper.GridView.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Queries
{
    public class GetDataQuery
    {
        public class Query : IRequest<(GetDataResponse, Exception?)>
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

        public class GetDataQueryHandler : IRequestHandler<Query, (GetDataResponse, Exception?)>
        {
            private readonly IGridViewContextModel gridViewContextModel;
            private readonly IEffectiveTraitModel effectiveTraitModel;
            private readonly ITraitsProvider traitsProvider;
            private readonly IModelContextBuilder modelContextBuilder;
            private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
            private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;

            public GetDataQueryHandler(IGridViewContextModel gridViewContextModel, IEffectiveTraitModel effectiveTraitModel,
                ITraitsProvider traitsProvider, IModelContextBuilder modelContextBuilder,
                ILayerBasedAuthorizationService layerBasedAuthorizationService, ICIBasedAuthorizationService ciBasedAuthorizationService
                )
            {
                this.gridViewContextModel = gridViewContextModel;
                this.effectiveTraitModel = effectiveTraitModel;
                this.traitsProvider = traitsProvider;
                this.modelContextBuilder = modelContextBuilder;
                this.layerBasedAuthorizationService = layerBasedAuthorizationService;
                this.ciBasedAuthorizationService = ciBasedAuthorizationService;
            }

            public async Task<(GetDataResponse, Exception?)> Handle(Query request, CancellationToken cancellationToken)
            {
                var validator = new QueryValidator();
                validator.ValidateAndThrow(request);

                var trans = modelContextBuilder.BuildImmediate();

                var config = await gridViewContextModel.GetConfiguration(request.Context, trans);
                
                var result = new GetDataResponse
                {
                    Rows = new List<Row>()
                };

                var activeTrait = await traitsProvider.GetActiveTrait(config.Trait, trans, TimeThreshold.BuildLatest());

                if (activeTrait == null)
                {
                    return (new GetDataResponse(), new Exception($"Active trait {config.Trait} was not found!"));
                }

                var res = await effectiveTraitModel.GetMergedCIsWithTrait(
                    activeTrait,
                    new LayerSet(config.ReadLayerset.ToArray()),
                    trans,
                    TimeThreshold.BuildLatest()
                    );

                foreach (var item in res)
                {
                    var ci_id = item.ID;

                    var canRead = ciBasedAuthorizationService.CanReadCI(ci_id);

                    if (!canRead)
                    {
                        continue;
                    }

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

                return (result, null);
            }
        }
    }
}
