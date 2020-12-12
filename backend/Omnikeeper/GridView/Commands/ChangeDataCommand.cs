using FluentValidation;
using MediatR;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GridView.Entity;
using Omnikeeper.GridView.Helper;
using Omnikeeper.GridView.Model;
using Omnikeeper.GridView.Request;
using Omnikeeper.GridView.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Commands
{
    public class ChangeDataCommand
    {
        public class Command : IRequest<(ChangeDataResponse, Exception?)>
        {
            public ChangeDataRequest Changes { get; set; }
            public string Context { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Context).NotEmpty().NotNull();
            }
        }

        public class ChangeDataCommandHandler : IRequestHandler<Command, (ChangeDataResponse, Exception?)>
        {
            private readonly ICIModel ciModel;
            private readonly IAttributeModel attributeModel;
            private readonly IChangesetModel changesetModel;
            private readonly ICurrentUserService currentUserService;
            private readonly IGridViewContextModel gridViewContextModel;
            private readonly IEffectiveTraitModel effectiveTraitModel;
            private readonly ITraitsProvider traitsProvider;
            private readonly IModelContextBuilder modelContextBuilder;
            private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
            private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;

            public ChangeDataCommandHandler(ICIModel ciModel, IAttributeModel attributeModel,
                IChangesetModel changesetModel, ICurrentUserService currentUserService, IGridViewContextModel gridViewContextModel,
                IEffectiveTraitModel effectiveTraitModel, ITraitsProvider traitsProvider, IModelContextBuilder modelContextBuilder,
                ILayerBasedAuthorizationService layerBasedAuthorizationService, ICIBasedAuthorizationService ciBasedAuthorizationService)
            {
                this.ciModel = ciModel;
                this.attributeModel = attributeModel;
                this.changesetModel = changesetModel;
                this.currentUserService = currentUserService;
                this.gridViewContextModel = gridViewContextModel;
                this.effectiveTraitModel = effectiveTraitModel;
                this.traitsProvider = traitsProvider;
                this.modelContextBuilder = modelContextBuilder;
                this.layerBasedAuthorizationService = layerBasedAuthorizationService;
                this.ciBasedAuthorizationService = ciBasedAuthorizationService;
            }
            public async Task<(ChangeDataResponse, Exception?)> Handle(Command request, CancellationToken cancellationToken)
            {
                var validator = new CommandValidator();

                var validation = validator.Validate(request);

                if (!validation.IsValid)
                {
                    return (new ChangeDataResponse(), ValidationHelper.CreateException(validation));
                }

                using var trans = modelContextBuilder.BuildDeferred();

                var user = await currentUserService.GetCurrentUser(trans);
                var changesetProxy = new ChangesetProxy(user.InDatabase, DateTimeOffset.Now, changesetModel);

                var config = await gridViewContextModel.GetConfiguration(request.Context, trans);

                if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, config.WriteLayer))
                    return (new ChangeDataResponse(), new Exception($"User \"{user.Username}\" does not have permission to write to layer ID {config.WriteLayer}"));

                foreach (var row in request.Changes.SparseRows)
                {
                    if (row.Ciid == null)
                    {
                        row.Ciid = await ciModel.CreateCI(trans);
                    } else
                    {
                        var ciExists = await ciModel.CIIDExists(row.Ciid.Value, trans);

                        if (!ciExists)
                        {
                            return (new ChangeDataResponse(), new Exception($"The provided ci id: {row.Ciid} was not found!"));
                        }
                    }


                    foreach (var cell in row.Cells)
                    {
                        var configItem = config.Columns.Find(item => item.SourceAttributeName == cell.Name);
                        if (configItem == null)
                        {
                            return (new ChangeDataResponse(), new Exception($"Could not find the supplied column {cell.Name} in the configuration"));
                        }

                        var writeLayer = configItem.WriteLayer != null ? configItem.WriteLayer.Value : config.WriteLayer;

                        if (!ciBasedAuthorizationService.CanWriteToCI(row.Ciid.Value))
                            return (new ChangeDataResponse(), new Exception($"User \"{user.Username}\" does not have permission to write to CI {row.Ciid.Value}"));

                        if (cell.Value == null)
                        {
                            try
                            {
                                await attributeModel.RemoveAttribute(
                                    cell.Name,
                                    row.Ciid.Value,
                                    writeLayer,
                                    changesetProxy,
                                    trans);
                            }
                            catch (Exception e)
                            {
                                trans.Rollback();
                                return (new ChangeDataResponse(), new Exception($"Removing attribute {cell.Name} for ci with id: {row.Ciid} failed!", e));
                            }
                        }
                        else
                        {
                            try
                            {
                                var val = AttributeValueBuilder.BuildFromDTO(new AttributeValueDTO
                                {
                                    IsArray = false,
                                    Values = new string[] { cell.Value },
                                    Type = AttributeValueType.Text
                                });

                                await attributeModel.InsertAttribute(
                                    cell.Name,
                                    val,
                                    row.Ciid.Value,
                                    writeLayer,
                                    changesetProxy,
                                    trans);
                            }
                            catch (Exception e)
                            {
                                trans.Rollback();
                                return (new ChangeDataResponse(), new Exception($"Inserting attribute {cell.Name} for ci with id: {row.Ciid} failed!", e));
                            }
                        }
                    }
                }

                var activeTrait = await traitsProvider.GetActiveTrait(config.Trait, trans, TimeThreshold.BuildLatest());
                if (activeTrait == null)
                    return (new ChangeDataResponse(), new Exception($"Could not find trait {config.Trait}"));

                var cisList = SpecificCIIDsSelection.Build(request.Changes.SparseRows.Select(i => i.Ciid.Value));
                var mergedCIs = await ciModel.GetMergedCIs(
                    cisList,
                    new LayerSet(config.ReadLayerset.ToArray()),
                    true,
                    trans,
                    TimeThreshold.BuildLatest()
                    );

                foreach (var mergedCI in mergedCIs)
                {
                    var hasTrait = await effectiveTraitModel.DoesCIHaveTrait(mergedCI, activeTrait, trans, TimeThreshold.BuildLatest());

                    if (!hasTrait)
                    {
                        trans.Rollback();
                        return (new ChangeDataResponse(), new Exception($"Consistency validation for CI with id={mergedCI.ID} failed. CI doesn't have the configured trait {activeTrait.Name}!"));
                    }
                }

                trans.Commit();
                return (BuildChangeResponse(mergedCIs, config), null);
            }

            private ChangeDataResponse BuildChangeResponse(IEnumerable<MergedCI> mergedCIs, GridViewConfiguration config)
            {
                using var trans = modelContextBuilder.BuildImmediate();

                var result = new ChangeDataResponse
                {
                    Rows = new List<ChangeDataRow>()
                };

                foreach (var item in mergedCIs)
                {
                    var ci_id = item.ID;


                    var canRead = ciBasedAuthorizationService.CanReadCI(ci_id);

                    if (!canRead)
                    {
                        continue;
                    }

                    foreach (var attr in item.MergedAttributes)
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
                            el.Cells.Add(new Response.ChangeDataCell
                            {
                                Name = name,
                                Value = attr.Value.Attribute.Value.Value2String(),
                                Changeable = col.WriteLayer != null
                            });
                        }
                        else
                        {
                            result.Rows.Add(new ChangeDataRow
                            {
                                Ciid = ci_id,
                                Cells = new List<Response.ChangeDataCell>
                                    {
                                        new Response.ChangeDataCell
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
