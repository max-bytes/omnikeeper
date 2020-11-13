using MediatR;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Commands
{
    public class DeleteContextCommand
    {
        public class Command : IRequest<bool>
        {
            public string Name { get; set; }
        }

        public class DeleteContextCommandHandler : IRequestHandler<Command, bool>
        {
            private readonly IGridViewConfigModel gridViewConfigModel;
            private readonly IModelContextBuilder modelContextBuilder;
            public DeleteContextCommandHandler(IGridViewConfigModel gridViewConfigModel, IModelContextBuilder modelContextBuilder)
            {
                this.gridViewConfigModel = gridViewConfigModel;
                this.modelContextBuilder = modelContextBuilder;
            }

            public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
            {
                using var trans = modelContextBuilder.BuildDeferred();

                var isSuccess = await gridViewConfigModel.DeleteContext(request.Name, trans);

                if (isSuccess)
                {
                    trans.Commit();
                    return true;
                }
                return false;
            }
        }
    }
}
