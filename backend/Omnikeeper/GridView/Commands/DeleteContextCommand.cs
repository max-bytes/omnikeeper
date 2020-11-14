using MediatR;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Commands
{
    public class DeleteContextCommand
    {
        public class Command : IRequest<Exception?>
        {
            public string Name { get; set; }
        }

        public class DeleteContextCommandHandler : IRequestHandler<Command, Exception?>
        {
            private readonly IGridViewConfigModel gridViewConfigModel;
            private readonly IModelContextBuilder modelContextBuilder;
            public DeleteContextCommandHandler(IGridViewConfigModel gridViewConfigModel, IModelContextBuilder modelContextBuilder)
            {
                this.gridViewConfigModel = gridViewConfigModel;
                this.modelContextBuilder = modelContextBuilder;
            }

            public async Task<Exception?> Handle(Command request, CancellationToken cancellationToken)
            {
                using var trans = modelContextBuilder.BuildDeferred();

                var isSuccess = await gridViewConfigModel.DeleteContext(request.Name, trans);

                if (isSuccess)
                {
                    trans.Commit();
                    return null;
                }
                return new Exception("An error occured deleting context");
            }
        }
    }
}
