﻿using FluentValidation;
using MediatR;
using Omnikeeper.Base.Authz;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GraphQL;
using Omnikeeper.GridView.Entity;
using Omnikeeper.GridView.Helper;
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
            private readonly GridViewContextModel gridViewContextModel;
            private readonly IEffectiveTraitModel effectiveTraitModel;
            private readonly IRelationModel relationModel;
            private readonly ICIModel ciModel;
            private readonly ITraitsHolder traitsHolder;
            private readonly IModelContextBuilder modelContextBuilder;
            private readonly IMetaConfigurationModel metaConfigurationModel;
            private readonly IAuthzFilterManager authzFilterManager;
            private readonly ICurrentUserAccessor currentUserService;

            public GetDataQueryHandler(GridViewContextModel gridViewContextModel, IEffectiveTraitModel effectiveTraitModel, IRelationModel relationModel, ICIModel ciModel,
                ITraitsHolder traitsHolder, IModelContextBuilder modelContextBuilder, IMetaConfigurationModel metaConfigurationModel,
                ICurrentUserAccessor currentUserService, IAuthzFilterManager authzFilterManager)
            {
                this.gridViewContextModel = gridViewContextModel;
                this.effectiveTraitModel = effectiveTraitModel;
                this.relationModel = relationModel;
                this.ciModel = ciModel;
                this.traitsHolder = traitsHolder;
                this.modelContextBuilder = modelContextBuilder;
                this.metaConfigurationModel = metaConfigurationModel;
                this.currentUserService = currentUserService;
                this.authzFilterManager = authzFilterManager;
            }

            public async Task<(GetDataResponse?, Exception?)> Handle(Query request, CancellationToken cancellationToken)
            {
                var validator = new QueryValidator();
                validator.ValidateAndThrow(request);

                var trans = modelContextBuilder.BuildImmediate();
                var user = await currentUserService.GetCurrentUser(trans);

                var atTime = TimeThreshold.BuildLatest();

                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
                var context = await gridViewContextModel.GetSingleByDataID(request.Context, metaConfiguration.ConfigLayerset, trans, atTime);
                if (context == default) return (null, new Exception($"Could not find context with ID {request.Context}"));
                var config = context.entity.Configuration;
                var readLayerset = new LayerSet(config.ReadLayerset);

                if (await authzFilterManager.ApplyFilterForQuery(new QueryOperationContext(), user, readLayerset, trans, atTime) is AuthzFilterResultDeny d)
                    return (null, new Exception(d.Reason));

                var activeTrait = traitsHolder.GetTrait(config.Trait);

                if (activeTrait == null)
                {
                    return (null, new Exception($"Active trait {config.Trait} was not found!"));
                }

                // reduce attribute fetching by selection
                var relevantAttributes = activeTrait.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name)
                    .Concat(config.Columns.Where(c => c.SourceAttributePath == null).Select(c => c.SourceAttributeName))
                    .ToHashSet();
                var attributeSelection = NamedAttributesSelection.Build(relevantAttributes);
                var mergedCIs = await ciModel.GetMergedCIs(AllCIIDsSelection.Instance, readLayerset, false, attributeSelection, trans, atTime);
                var mergedCIsWithTrait = effectiveTraitModel.FilterCIsWithTrait(mergedCIs, activeTrait, readLayerset);

                var attributeResolver = new AttributeResolver();
                await attributeResolver.PrefetchRelatedCIsAndLookups(config, mergedCIsWithTrait.Select(ci => ci.ID).ToHashSet(), relationModel, ciModel, trans, atTime);

                var resultRows = new Dictionary<Guid, Row>();

                foreach (var item in mergedCIsWithTrait)
                {
                    var ci_id = item.ID;

                    var filteredColumns = config.Columns.Select(column =>
                    {
                        if (attributeResolver.TryResolveAttribute(item, column, out var attribute))
                        {
                            return ((GridViewColumn column, MergedCIAttribute? attr))(column, attribute);
                        }
                        else
                        {
                            return (column, null);
                        }
                    });

                    foreach (var (column, attr) in filteredColumns)
                    {
                        bool changable = true;
                        if (attr != null)
                        {
                            if (attr.LayerStackIDs.Count > 1)
                            {
                                if (attr.LayerStackIDs.First() != config.WriteLayer)
                                {
                                    changable = false;
                                }
                            }
                        }

                        var value = (attr != null)
                            ? AttributeValueDTO.Build(attr.Attribute.Value)
                            : AttributeValueDTO.BuildEmpty(column.ValueType ?? AttributeValueType.Text, false);

                        var cell = new Cell(
                                GridViewColumn.GenerateColumnID(column),
                                value,
                                column.WriteLayer == null ? true : (column.WriteLayer != "") && changable
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
