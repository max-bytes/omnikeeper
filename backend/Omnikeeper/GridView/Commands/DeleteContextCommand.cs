using MediatR;
using Omnikeeper.Base.Model;
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
            public DeleteContextCommandHandler(IGridViewConfigModel gridViewConfigModel)
            {
                this.gridViewConfigModel = gridViewConfigModel;
            }

            public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
            {
                var isSuccess = await gridViewConfigModel.DeleteContext(request.Name);

                return isSuccess;
            }
        }
    }
}
