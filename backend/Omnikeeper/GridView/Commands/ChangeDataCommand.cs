using MediatR;
using Npgsql;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GridView.Request;
using Omnikeeper.GridView.Response;
using Omnikeeper.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Commands
{
    public class ChangeDataCommand
    {
        public class Command : IRequest<ChangeDataResponse>
        {
            public ChangeDataRequest Changes { get; set; }
        }

        public class ChangeDataCommandHandler : IRequestHandler<Command, ChangeDataResponse>
        {
            private readonly NpgsqlConnection conn;
            private readonly ICIModel ciModel;
            private readonly IAttributeModel attributeModel;
            private readonly IChangesetModel changesetModel;
            private readonly ICurrentUserService currentUserService;
            public ChangeDataCommandHandler(NpgsqlConnection connection, ICIModel ciModel, IAttributeModel attributeModel, IChangesetModel changesetModel, ICurrentUserService currentUserService)
            {
                conn = connection;
                this.ciModel = ciModel;
                this.attributeModel = attributeModel;
                this.changesetModel = changesetModel;
                this.currentUserService = currentUserService;
            }
            public async Task<ChangeDataResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await currentUserService.GetCurrentUser(null);
                var changesetProxy = ChangesetProxy.Build(user.InDatabase, DateTimeOffset.Now, changesetModel);
                // check if ci with provided id exists
                // we need layer id for this

                // we should do all changes in a single transaction

                foreach (var row in request.Changes.SparseRows)
                {
                    var ciExists = await ciModel.CIIDExists(row.Ciid, null);

                    if (ciExists)
                    {
                        // add attributes for this ci for this ci

                        using (var trans = conn.BeginTransaction())
                        {
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

                                    var a = await attributeModel.InsertAttribute(
                                        cell.Name,
                                        val,
                                        row.Ciid,
                                        1, // how to get this layer id ??
                                        changesetProxy,
                                        trans);
                                }

                                trans.Commit();
                            }
                            catch (Exception ex)
                            {
                                trans.Rollback();
                            }

                        }


                    }
                }


                var result = await FetchData();
                return result;
            }

            private async Task<ChangeDataResponse> FetchData()
            {
                var result = new ChangeDataResponse();

                return result;
            }
        }
    }
}
