﻿using FluentValidation;
using MediatR;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GridView.Entity;
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


                var activeTrait = await traitsProvider.GetActiveTrait(config.Trait, trans, TimeThreshold.BuildLatest());

                if (activeTrait == null)
                {
                    return (null, new Exception($"Active trait {config.Trait} was not found!"));
                }

                var res = await effectiveTraitModel.GetMergedCIsWithTrait(
                    activeTrait,
                    new LayerSet(config.ReadLayerset), new AllCIIDsSelection(),
                    trans,
                    TimeThreshold.BuildLatest()
                    );


                var resultRows = new Dictionary<Guid, Row>();

                // filter readable CIs based on authorization
                var filteredCIs = ciBasedAuthorizationService.FilterReadableCIs(res, (t) => t.ID);

                foreach (var item in filteredCIs)
                {
                    var ci_id = item.ID;

                    var filteredColumns = config.Columns.Select(column =>
                    {
                        if (item.MergedAttributes.TryGetValue(column.SourceAttributeName, out var attribute))
                        {
                            return ((GridViewColumn column, MergedCIAttribute? attr))(column, attribute);
                        } else
                        {
                            return (column, null);
                        }
                    });

                    foreach (var (column, attr) in filteredColumns)
                    {
                        bool changable = true;
                        if (attr != null)
                        {
                            if (attr.LayerStackIDs.Length > 1)
                            {
                                if (attr.LayerStackIDs[^1] != config.WriteLayer)
                                {
                                    changable = false;
                                }
                            }
                        }

                        var value = (attr != null) 
                            ? AttributeValueDTO.Build(attr.Attribute.Value) 
                            : AttributeValueDTO.BuildEmpty(column.ValueType ?? AttributeValueType.Text, false);

                        var cell = new Cell(
                                column.SourceAttributeName,
                                value,
                                column.WriteLayer == null ? true : (column.WriteLayer != -1) && changable
                            );

                        if (resultRows.TryGetValue(ci_id, out var el))
                        {
                            el.Cells.Add(cell);
                        }
                        else
                        {
                            resultRows.Add(ci_id, new Row
                            (
                                ci_id,
                                new List<Cell> { cell }
                            ));
                        }
                    }
                }

                var result = new GetDataResponse(resultRows.Values);
                return (result, null);
            }
        }
    }
}
