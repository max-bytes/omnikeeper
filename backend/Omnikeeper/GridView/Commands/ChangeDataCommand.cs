using FluentValidation;
using MediatR;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Entity.GridView;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GridView.Helper;
using Omnikeeper.GridView.Request;
using Omnikeeper.GridView.Response;
using Omnikeeper.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Commands
{
    public class ChangeDataCommand
    {
        public class Command : IRequest<(ChangeDataResponse, bool, string)>
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

        public class ChangeDataCommandHandler : IRequestHandler<Command, (ChangeDataResponse, bool, string)>
        {
            private readonly NpgsqlConnection conn;
            private readonly ICIModel ciModel;
            private readonly IAttributeModel attributeModel;
            private readonly IChangesetModel changesetModel;
            private readonly ICurrentUserService currentUserService;
            private readonly IGridViewConfigModel gridViewConfigModel;
            private readonly IEffectiveTraitModel effectiveTraitModel;
            private readonly ITraitsProvider traitsProvider;
            public ChangeDataCommandHandler(NpgsqlConnection connection, ICIModel ciModel, IAttributeModel attributeModel,
                IChangesetModel changesetModel, ICurrentUserService currentUserService, IGridViewConfigModel gridViewConfigModel,
                IEffectiveTraitModel effectiveTraitModel, ITraitsProvider traitsProvider)
            {
                conn = connection;
                this.ciModel = ciModel;
                this.attributeModel = attributeModel;
                this.changesetModel = changesetModel;
                this.currentUserService = currentUserService;
                this.gridViewConfigModel = gridViewConfigModel;
                this.effectiveTraitModel = effectiveTraitModel;
                this.traitsProvider = traitsProvider;
            }
            public async Task<(ChangeDataResponse, bool, string)> Handle(Command request, CancellationToken cancellationToken)
            {
                var validator = new CommandValidator();

                var validation = validator.Validate(request);

                if (!validation.IsValid)
                {
                    return (new ChangeDataResponse(), false, ValidationHelper.CreateErrorMessage(validation));
                }

                var user = await currentUserService.GetCurrentUser(null);
                var changesetProxy = ChangesetProxy.Build(user.InDatabase, DateTimeOffset.Now, changesetModel);

                // TO DO:
                // The consistency validation per CI consists 
                // of checking whether or not the CI still fulfills/has the configured trait.

                /* NOTE mcsuk: 
                 * I'd structure this in the following way, hope that works:
                 * begin transaction
                 * foreach changed CI
                 *   perform all the changes for this CI using AttributeModel methods
                 * end foreach
                 * fetch all changed CIs using CIModel.GetMergedCIs()
                 * foreach MergedCI
                 *   check if it still has trait via EffectiveTraitModel.DoesCIHaveTrait()
                 *   if it does not, rollback transaction and return error
                 * end foreach
                 * commit transaction
                 * return changed CIs/rows, re-using fetched CIs from above as data basis
                 */


                var config = await gridViewConfigModel.GetConfiguration(request.Context);

                using var trans = conn.BeginTransaction();
                foreach (var row in request.Changes.SparseRows)
                {
                    var ciExists = await ciModel.CIIDExists(row.Ciid, null);

                    if (!ciExists)
                    {
                        return (new ChangeDataResponse(), false, $"The provided ci id: {row.Ciid} was not found!");
                    }

                    foreach (var cell in row.Cells)
                    {
                        var configItem = config.Columns.Find(item => item.SourceAttributeName == cell.Name);

                        var writeLayer = configItem.WriteLayer != null ? configItem.WriteLayer.Value : config.WriteLayer;

                        if (cell.Value == null)
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
                            catch (Exception)
                            {
                                trans.Rollback();
                                return (new ChangeDataResponse(), false, $"Removing attribute {cell.Name} for ci with id: {row.Ciid} failed!");
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
                                    row.Ciid,
                                    writeLayer,
                                    changesetProxy,
                                    trans);
                            }
                            catch
                            {
                                trans.Rollback();
                                return (new ChangeDataResponse(), false, $"Inserting attribute {cell.Name} for ci with id: {row.Ciid} failed!");
                            }
                        }

                    }


                }

                var cisList = SpecificCIIDsSelection.Build(request.Changes.SparseRows.Select(i => i.Ciid));
                var activeTrait = await traitsProvider.GetActiveTrait(config.Trait, null, TimeThreshold.BuildLatest());

                var mergedCIs = await ciModel.GetMergedCIs(
                    cisList,
                    new LayerSet(config.ReadLayerset.ToArray()),
                    true,
                    null,
                    TimeThreshold.BuildLatest()
                    );

                foreach (var mergedCI in mergedCIs)
                {
                    var hasTrait = await effectiveTraitModel.DoesCIHaveTrait(mergedCI, activeTrait, null, TimeThreshold.BuildLatest());

                    if (!hasTrait)
                    {
                        trans.Rollback();
                        return (new ChangeDataResponse(), false, $"Consistency validation for CI with id={mergedCI.ID} failed. CI doesn't has the configured trait {activeTrait.Name}!");
                    }
                }

                trans.Commit();
                return (await FetchData(config), true, "");
            }

            private async Task<ChangeDataResponse> FetchData(GridViewConfiguration config)
            {
                var result = new ChangeDataResponse
                {
                    Rows = new List<ChangeDataRow>()
                };

                var activeTrait = await traitsProvider.GetActiveTrait(config.Trait, null, TimeThreshold.BuildLatest());
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
