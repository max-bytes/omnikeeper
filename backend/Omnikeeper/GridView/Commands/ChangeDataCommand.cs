using FluentValidation;
using MediatR;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GridView.Entity;
using Omnikeeper.GridView.Helper;
using Omnikeeper.GridView.Request;
using Omnikeeper.GridView.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Omnikeeper.Base.Authz;
using Omnikeeper.Authz;

namespace Omnikeeper.GridView.Commands
{
    public class ChangeDataCommand
    {
        public class Command : IRequest<(ChangeDataResponse?, Exception?)>
        {
            public ChangeDataRequest Changes { get; set; }
            public string Context { get; set; }

            public Command(ChangeDataRequest Changes, string Context)
            {
                this.Changes = Changes;
                this.Context = Context;
            }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Context).NotEmpty().NotNull();
            }
        }

        public class ChangeDataCommandHandler : IRequestHandler<Command, (ChangeDataResponse?, Exception?)>
        {
            private readonly ICIModel ciModel;
            private readonly IAttributeModel attributeModel;
            private readonly IRelationModel relationModel;
            private readonly IChangesetModel changesetModel;
            private readonly ICurrentUserAccessor currentUserService;
            private readonly GridViewContextModel gridViewContextModel;
            private readonly IEffectiveTraitModel effectiveTraitModel;
            private readonly ITraitsProvider traitsProvider;
            private readonly IModelContextBuilder modelContextBuilder;
            private readonly IAuthzFilterManager authzFilterManager;
            private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;
            private readonly IMetaConfigurationModel metaConfigurationModel;

            public ChangeDataCommandHandler(ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel,
                IChangesetModel changesetModel, ICurrentUserAccessor currentUserService, GridViewContextModel gridViewContextModel,
                IEffectiveTraitModel effectiveTraitModel, ITraitsProvider traitsProvider, IModelContextBuilder modelContextBuilder,
                IAuthzFilterManager authzFilterManager, ICIBasedAuthorizationService ciBasedAuthorizationService,
                IMetaConfigurationModel metaConfigurationModel)
            {
                this.ciModel = ciModel;
                this.attributeModel = attributeModel;
                this.relationModel = relationModel;
                this.changesetModel = changesetModel;
                this.currentUserService = currentUserService;
                this.gridViewContextModel = gridViewContextModel;
                this.effectiveTraitModel = effectiveTraitModel;
                this.traitsProvider = traitsProvider;
                this.modelContextBuilder = modelContextBuilder;
                this.authzFilterManager = authzFilterManager;
                this.ciBasedAuthorizationService = ciBasedAuthorizationService;
                this.metaConfigurationModel = metaConfigurationModel;
            }
            public async Task<(ChangeDataResponse?, Exception?)> Handle(Command request, CancellationToken cancellationToken)
            {
                var validator = new CommandValidator();

                var validation = validator.Validate(request);

                if (!validation.IsValid)
                {
                    return (null, ValidationHelper.CreateException(validation));
                }

                using var trans = modelContextBuilder.BuildDeferred();

                var timeThreshold = TimeThreshold.BuildLatest();
                var user = await currentUserService.GetCurrentUser(trans);
                var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel);

                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);

                var context = await gridViewContextModel.GetSingleByDataID(request.Context, metaConfiguration.ConfigLayerset, trans, timeThreshold);
                if (context == default) return (null, new Exception($"Could not find context with ID {request.Context}"));
                var config = context.entity.Configuration;

                if (await authzFilterManager.ApplyPreFilterForMutation(MutationOperation.MutateCIs, user, config.ReadLayerset, config.WriteLayer) is string reason)
                    return (null, new Exception(reason));

                foreach (var row in request.Changes.SparseRows)
                {
                    var ciExists = await ciModel.CIIDExists(row.Ciid, trans);
                    if (!ciExists)
                    {
                        row.Ciid = await ciModel.CreateCI(row.Ciid, trans);
                    }

                    foreach (var cell in row.Cells)
                    {
                        var configItem = config.Columns.Find(item => GridViewColumn.GenerateColumnID(item) == cell.ID);
                        if (configItem == null)
                        {
                            return (null, new Exception($"Could not find the supplied column with ID \"{cell.ID}\" in the configuration"));
                        }

                        if (configItem.SourceAttributePath != null)
                            return (null, new Exception($"Modifying attributes whose column are configured via SourceAttributePath is not supported (yet)"));
                        var attributeName = configItem.SourceAttributeName;

                        string writeLayer;

                        if (configItem.WriteLayer == null)
                        {
                            writeLayer = config.WriteLayer;
                        }
                        else if (configItem.WriteLayer == "")
                        {
                            return (null, new Exception($"Provided column with ID \"{cell.ID}\" is not writable!"));
                        }
                        else
                        {
                            writeLayer = configItem.WriteLayer;
                        }
                        // TODO: proper check for write permissions

                        //var writeLayer = configItem.WriteLayer != null ? configItem.WriteLayer.Value : config.WriteLayer;

                        if (!ciBasedAuthorizationService.CanWriteToCI(row.Ciid))
                            return (null, new Exception($"User \"{user.Username}\" does not have permission to write to CI {row.Ciid}"));

                        if (cell.Value.Values.IsEmpty())
                        {
                            // we treat an empty values array as a request to remove the attribute, but only if the CI already exists
                            if (ciExists)
                            {
                                try
                                {
                                    // TODO: mask handling
                                    await attributeModel.RemoveAttribute(
                                        attributeName,
                                        row.Ciid,
                                        writeLayer,
                                        changesetProxy,
                                        new DataOriginV1(DataOriginType.Manual),
                                        trans,
                                        MaskHandlingForRemovalApplyNoMask.Instance);
                                }
                                catch (Exception e)
                                {
                                    trans.Rollback();
                                    return (null, new Exception($"Removing attribute {attributeName} for ci with id: {row.Ciid} failed!", e));
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                var val = AttributeValueHelper.BuildFromDTO(cell.Value);

                                // TODO: other-layers-value handling
                                await attributeModel.InsertAttribute(
                                    attributeName,
                                    val,
                                    row.Ciid,
                                    writeLayer,
                                    changesetProxy,
                                    new DataOriginV1(DataOriginType.Manual),
                                    trans,
                                    OtherLayersValueHandlingForceWrite.Instance);
                            }
                            catch (Exception e)
                            {
                                trans.Rollback();
                                return (null, new Exception($"Inserting attribute {attributeName} for ci with id: {row.Ciid} failed!", e));
                            }
                        }
                    }
                }

                var activeTrait = await traitsProvider.GetActiveTrait(config.Trait, trans, timeThreshold);
                if (activeTrait == null)
                    return (null, new Exception($"Could not find trait {config.Trait}"));

                var cisList = SpecificCIIDsSelection.Build(request.Changes.SparseRows.Select(i => i.Ciid).ToHashSet());
                // TODO: only fetch relevant attributes
                var mergedCIs = await ciModel.GetMergedCIs(cisList, new LayerSet(config.ReadLayerset.ToArray()), true, AllAttributeSelection.Instance, trans, timeThreshold);

                var cisWithTrait = effectiveTraitModel.FilterCIsWithTrait(mergedCIs, activeTrait, new LayerSet(config.ReadLayerset.ToArray()), trans, timeThreshold);
                if (cisWithTrait.Count() < mergedCIs.Count())
                {
                    var cisWithoutTrait = mergedCIs.Select(ci => ci.ID).ToHashSet().Except(cisWithTrait.Select(ci => ci.ID));
                    trans.Rollback();
                    return (null, new Exception($"Consistency validation for CI with id={cisWithoutTrait.FirstOrDefault()} failed. CI doesn't have the configured trait {activeTrait.ID}!"));
                }

                if (await authzFilterManager.ApplyPostFilterForMutation(MutationOperation.TraitEntities_RemoveRelations, user, changesetProxy) is string reasonPost)
                    return (null, new Exception(reasonPost));

                trans.Commit();
                return (await BuildChangeResponse(mergedCIs, config), null);
            }

            private async Task<ChangeDataResponse> BuildChangeResponse(IEnumerable<MergedCI> mergedCIs, GridViewConfiguration config)
            {
                using var trans = modelContextBuilder.BuildImmediate();
                var timeThreshold = TimeThreshold.BuildLatest();

                var result = new ChangeDataResponse(new List<ChangeDataRow>());

                var attributeResolver = new AttributeResolver();
                await attributeResolver.PrefetchRelatedCIsAndLookups(config, mergedCIs.Select(ci => ci.ID).ToHashSet(), relationModel, ciModel, trans, timeThreshold);

                foreach (var item in mergedCIs)
                {
                    var ci_id = item.ID;

                    var canWrite = ciBasedAuthorizationService.CanWriteToCI(ci_id); // TODO: refactor to use a method that queries all ciids at once, returning those that are readable

                    if (!canWrite)
                    {
                        continue;
                    }

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

                        var cell = new Response.ChangeDataCell(
                                GridViewColumn.GenerateColumnID(column),
                                value,
                                column.WriteLayer == null ? true : (column.WriteLayer != "") && changable
                            );

                        var el = result.Rows.Find(el => el.Ciid == ci_id);
                        if (el != null)
                        {
                            el.Cells.Add(cell);
                        }
                        else
                        {
                            result.Rows.Add(new ChangeDataRow
                            (
                                ci_id,
                                new List<Response.ChangeDataCell> { cell }
                            ));
                        }
                    }
                }

                return result;
            }
        }
    }
}
