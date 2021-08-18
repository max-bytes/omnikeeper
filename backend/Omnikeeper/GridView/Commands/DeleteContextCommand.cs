using MediatR;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Model;
using Omnikeeper.GridView.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Commands
{
    public class DeleteContextCommand
    {
        public class Command : IRequest<Exception?>
        {
            public string ID { get; set; }

            public Command(string id)
            {
                this.ID = id;
            }
        }

        public class DeleteContextCommandHandler : IRequestHandler<Command, Exception?>
        {
            private readonly IGridViewContextWriteService gridViewContextWriteService;
            private readonly IModelContextBuilder modelContextBuilder;
            private readonly ICurrentUserService currentUserService;
            private readonly IChangesetModel changesetModel;
            public DeleteContextCommandHandler(IModelContextBuilder modelContextBuilder, IGridViewContextWriteService gridViewContextWriteService, ICurrentUserService currentUserService, IChangesetModel changesetModel)
            {
                this.modelContextBuilder = modelContextBuilder;
                this.gridViewContextWriteService = gridViewContextWriteService;
                this.currentUserService = currentUserService;
                this.changesetModel = changesetModel;
            }

            public async Task<Exception?> Handle(Command request, CancellationToken cancellationToken)
            {
                var timeThreshold = TimeThreshold.BuildLatest();
                using var trans = modelContextBuilder.BuildDeferred();
                var user = await currentUserService.GetCurrentUser(trans);
                var changesetProxy = new ChangesetProxy(user.InDatabase, timeThreshold, changesetModel);

                try
                {
                    var isSuccess = await gridViewContextWriteService.TryToDelete(request.ID, new Base.Entity.DataOrigin.DataOriginV1(Base.Entity.DataOrigin.DataOriginType.Manual), changesetProxy, user, trans);

                    if (isSuccess)
                    {
                        trans.Commit();
                        return null;
                    } else
                    {
                        return new Exception("An error occured deleting context");
                    }
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }
        }
    }
}
