﻿using FluentValidation;
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
        public class Query : IRequest<(GetDataResponse?, Exception?)>
        {
            public string Context { get; set; }

            public Query(string Context)
            {
                this.Context = Context;
            }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.Context).NotEmpty();
            }
        }

        public class GetDataQueryHandler : IRequestHandler<Query, (GetDataResponse?, Exception?)>
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

            public async Task<(GetDataResponse?, Exception?)> Handle(Query request, CancellationToken cancellationToken)
            {
                var validator = new QueryValidator();
                validator.ValidateAndThrow(request);

                var trans = modelContextBuilder.BuildImmediate();

                var config = await gridViewContextModel.GetConfiguration(request.Context, trans);

                var result = new GetDataResponse(new List<Row>());

                var activeTrait = await traitsProvider.GetActiveTrait(config.Trait, trans, TimeThreshold.BuildLatest());

                if (activeTrait == null)
                {
                    return (null, new Exception($"Active trait {config.Trait} was not found!"));
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
                        bool changable = true;

                        if (col == null)
                        {
                            continue;
                        }

                        if (attr.Value.LayerStackIDs.Length > 1)
                        {
                            if (attr.Value.LayerStackIDs[^1] != config.WriteLayer)
                            {
                                changable = false;
                            }
                        }


                        var el = result.Rows.Find(el => el.Ciid == ci_id);

                        if (el != null)
                        {
                            el.Cells.Add(new Cell(
                                name,
                                attr.Value.Attribute.Value.Value2String(),
                                col.WriteLayer == null ? true : (col.WriteLayer != -1) && changable 
                            ));
                        }
                        else
                        {
                            result.Rows.Add(new Row
                            (
                                ci_id,
                                new List<Cell>
                                    {
                                        new Cell(
                                            name, 
                                            attr.Value.Attribute.Value.Value2String(),
                                            col.WriteLayer == null ? true : (col.WriteLayer != -1) && changable
                                        )
                                    }
                            ));
                        }
                    }
                }

                return (result, null);
            }
        }
    }
}
