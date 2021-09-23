using FluentValidation;
using MediatR;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GridView.Entity;
using Omnikeeper.GridView.Helper;
using Omnikeeper.GridView.Model;
using Omnikeeper.GridView.Response;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
            private readonly IRelationModel relationModel;
            private readonly ICIModel ciModel;
            private readonly ITraitsProvider traitsProvider;
            private readonly IModelContextBuilder modelContextBuilder;
            private readonly IBaseConfigurationModel baseConfigurationModel;
            private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
            private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;
            private readonly ICurrentUserService currentUserService;

            public GetDataQueryHandler(IGridViewContextModel gridViewContextModel, IEffectiveTraitModel effectiveTraitModel, IRelationModel relationModel, ICIModel ciModel,
                ITraitsProvider traitsProvider, IModelContextBuilder modelContextBuilder, IBaseConfigurationModel baseConfigurationModel,
                ILayerBasedAuthorizationService layerBasedAuthorizationService, ICIBasedAuthorizationService ciBasedAuthorizationService, ICurrentUserService currentUserService)
            {
                this.gridViewContextModel = gridViewContextModel;
                this.effectiveTraitModel = effectiveTraitModel;
                this.relationModel = relationModel;
                this.ciModel = ciModel;
                this.traitsProvider = traitsProvider;
                this.modelContextBuilder = modelContextBuilder;
                this.baseConfigurationModel = baseConfigurationModel;
                this.layerBasedAuthorizationService = layerBasedAuthorizationService;
                this.ciBasedAuthorizationService = ciBasedAuthorizationService;
                this.currentUserService = currentUserService;
            }

            public async Task<(GetDataResponse?, Exception?)> Handle(Query request, CancellationToken cancellationToken)
            {
                var validator = new QueryValidator();
                validator.ValidateAndThrow(request);

                var trans = modelContextBuilder.BuildImmediate();
                var user = await currentUserService.GetCurrentUser(trans);

                var atTime = TimeThreshold.BuildLatest();

                var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
                var context = await gridViewContextModel.GetFullContext(request.Context, new LayerSet(baseConfiguration.ConfigLayerset), atTime, trans);
                var config = context.Configuration;

                var activeTrait = await traitsProvider.GetActiveTrait(config.Trait, trans, atTime);

                if (activeTrait == null)
                {
                    return (null, new Exception($"Active trait {config.Trait} was not found!"));
                }

                if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, config.ReadLayerset))
                    return (null, new Exception($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', config.ReadLayerset)}"));

                // TODO: reduce attribute fetching by selection
                var mergedCIs = await ciModel.GetMergedCIs(new AllCIIDsSelection(), new LayerSet(config.ReadLayerset), false, AllAttributeSelection.Instance, trans, atTime);
                var mergedCIsWithTrait = await effectiveTraitModel.FilterCIsWithTrait(mergedCIs, activeTrait, new LayerSet(config.ReadLayerset), trans, atTime);

                // filter readable CIs based on authorization
                var filteredCIs = ciBasedAuthorizationService.FilterReadableCIs(mergedCIsWithTrait, (t) => t.ID);

                var attributeResolver = new AttributeResolver();
                await attributeResolver.PrefetchRelatedCIsAndLookups(config, filteredCIs.Select(ci => ci.ID).ToHashSet(), relationModel, ciModel, trans, atTime);

                var resultRows = new Dictionary<Guid, Row>();

                foreach (var item in filteredCIs)
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
