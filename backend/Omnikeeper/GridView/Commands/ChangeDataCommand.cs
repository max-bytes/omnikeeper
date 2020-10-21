using MediatR;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GridView.Model;
using Omnikeeper.GridView.Request;
using Omnikeeper.GridView.Response;
using Omnikeeper.GridView.Service;
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
        public class Command : IRequest<(ChangeDataResponse, bool)>
        {
            public ChangeDataRequest Changes { get; set; }
            public string ConfigurationName { get; set; }
        }

        public class ChangeDataCommandHandler : IRequestHandler<Command, (ChangeDataResponse, bool)>
        {
            private readonly NpgsqlConnection conn;
            private readonly ICIModel ciModel;
            private readonly IAttributeModel attributeModel;
            private readonly IChangesetModel changesetModel;
            private readonly ICurrentUserService currentUserService;
            private readonly GridViewConfigService gridViewConfigService;
            public ChangeDataCommandHandler(NpgsqlConnection connection, ICIModel ciModel, IAttributeModel attributeModel,
                IChangesetModel changesetModel, ICurrentUserService currentUserService, GridViewConfigService gridViewConfigService)
            {
                conn = connection;
                this.ciModel = ciModel;
                this.attributeModel = attributeModel;
                this.changesetModel = changesetModel;
                this.currentUserService = currentUserService;
                this.gridViewConfigService = gridViewConfigService;
            }
            public async Task<(ChangeDataResponse, bool)> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await currentUserService.GetCurrentUser(null);
                var changesetProxy = ChangesetProxy.Build(user.InDatabase, DateTimeOffset.Now, changesetModel);

                // check if ci with provided id exists
                // we need layer id for this

                // we should do all changes in a single transaction

                var config = await gridViewConfigService.GetConfiguration(request.ConfigurationName);

                foreach (var row in request.Changes.SparseRows)
                {
                    var ciExists = await ciModel.CIIDExists(row.Ciid, null);

                    if (!ciExists)
                    {
                        return (new ChangeDataResponse(), false);
                    }

                    using var trans = conn.BeginTransaction();
                    try
                    {
                        foreach (var cell in row.Cells)
                        {
                            var val = AttributeValueBuilder.Build(new AttributeValueDTO
                            {
                                IsArray = false,
                                Values = new string[] { cell.Value },
                                Type = AttributeValueType.Text
                            });

                            var writeLayer = cell.WriteLayer != null ? cell.WriteLayer.Value : config.WriteLayer;

                            await attributeModel.InsertAttribute(
                                cell.Name,
                                val,
                                row.Ciid,
                                writeLayer,
                                changesetProxy,
                                trans);
                        }

                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                    }
                }

                return (await FetchData(config), true);
            }

            private async Task<ChangeDataResponse> FetchData(GridViewConfiguration config)
            {
                var result = new ChangeDataResponse
                {
                    Rows = new List<ChangeDataRow>()
                };

                var ciIds = await ciModel.GetCIIDs(null);

                var attributes = new List<CIAttribute>();

                foreach (var layerId in config.ReadLayerset)
                {
                    var attrs = await attributeModel.GetAttributes(SpecificCIIDsSelection.Build(ciIds), layerId, null, TimeThreshold.BuildLatest());
                    attributes.AddRange(attrs);
                }

                foreach (var attribute in attributes)
                {
                    var col = config.Columns.Find(el => el.SourceAttributeName == attribute.Name);

                    if (col == null)
                    {
                        continue;
                    }

                    var el = result.Rows.Find(el => el.Ciid == attribute.CIID);

                    if (el != null)
                    {
                        el.Cells.Add(new Response.ChangeDataCell
                        {
                            Name = attribute.Name,
                            Value = attribute.Value.ToString(),
                            Changeable = col.WriteLayer != null
                        });
                    }
                    else
                    {
                        result.Rows.Add(new ChangeDataRow
                        {
                            Ciid = attribute.CIID,
                            Cells = new List<Response.ChangeDataCell>
                                    {
                                        new Response.ChangeDataCell
                                        {
                                            Name = attribute.Name,
                                            Value = attribute.Value.ToString(),
                                            Changeable = col.WriteLayer != null
                                        }
                                    }
                        });
                    }
                }

                return result;
            }
        }
    }
}
