﻿using FluentValidation;
using MediatR;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
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

                var config = await gridViewContextModel.GetConfiguration(request.Context, trans);

                if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, config.WriteLayer))
                    return (null, new Exception($"User \"{user.Username}\" does not have permission to write to layer ID {config.WriteLayer}"));

                foreach (var row in request.Changes.SparseRows)
                {
                    var ciExists = await ciModel.CIIDExists(row.Ciid, trans);
                    if (!ciExists)
                    {
                        row.Ciid = await ciModel.CreateCI(row.Ciid, trans);
                    }

                    foreach (var cell in row.Cells)
                    {
                        var configItem = config.Columns.Find(item => item.SourceAttributeName == cell.Name);
                        if (configItem == null)
                        {
                            return (null, new Exception($"Could not find the supplied column {cell.Name} in the configuration"));
                        }

                        long writeLayer;

                        if (configItem.WriteLayer == null)
                        {
                            writeLayer = config.WriteLayer;
                        } else if (configItem.WriteLayer!.Value == -1)
                        {
                            return (null, new Exception($"Provided column {cell.Name} is not writable!"));
                        } else
                        {
                            writeLayer = configItem.WriteLayer.Value;
                        }

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
                                    await attributeModel.RemoveAttribute(
                                        cell.Name,
                                        row.Ciid,
                                        writeLayer,
                                        changesetProxy,
                                        trans);
                                }
                                catch (Exception e)
                                {
                                    trans.Rollback();
                                    return (null, new Exception($"Removing attribute {cell.Name} for ci with id: {row.Ciid} failed!", e));
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                var val = AttributeValueBuilder.BuildFromDTO(cell.Value);

                                await attributeModel.InsertAttribute(
                                    cell.Name,
                                    val,
                                    row.Ciid,
                                    writeLayer,
                                    changesetProxy,
                                    new DataOriginV1(DataOriginType.Manual),
                                    trans);
                            }
                            catch (Exception e)
                            {
                                trans.Rollback();
                                return (null, new Exception($"Inserting attribute {cell.Name} for ci with id: {row.Ciid} failed!", e));
                            }
                        }
                    }
                }

                var activeTrait = await traitsProvider.GetActiveTrait(config.Trait, trans, timeThreshold);
                if (activeTrait == null)
                    return (null, new Exception($"Could not find trait {config.Trait}"));

                var cisList = SpecificCIIDsSelection.Build(request.Changes.SparseRows.Select(i => i.Ciid).ToHashSet());
                var mergedCIs = await ciModel.GetMergedCIs(
                    cisList,
                    new LayerSet(config.ReadLayerset.ToArray()),
                    true,
                    trans,
                    timeThreshold
                    );

                foreach (var mergedCI in mergedCIs)
                {
                    var hasTrait = await effectiveTraitModel.DoesCIHaveTrait(mergedCI, activeTrait, trans, timeThreshold);

                    if (!hasTrait)
                    {
                        trans.Rollback();
                        return (null, new Exception($"Consistency validation for CI with id={mergedCI.ID} failed. CI doesn't have the configured trait {activeTrait.Name}!"));
                    }
                }

                trans.Commit();
                return (BuildChangeResponse(mergedCIs, config), null);
            }

            private ChangeDataResponse BuildChangeResponse(IEnumerable<MergedCI> mergedCIs, GridViewConfiguration config)
            {
                using var trans = modelContextBuilder.BuildImmediate();

                var result = new ChangeDataResponse(new List<ChangeDataRow>());

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
                        if (item.MergedAttributes.TryGetValue(column.SourceAttributeName, out var attribute))
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

                        var cell = new Response.ChangeDataCell(
                                column.SourceAttributeName,
                                value,
                                column.WriteLayer == null ? true : (column.WriteLayer != -1) && changable
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