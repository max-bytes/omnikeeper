using MediatR;
using Omnikeeper.Base.Model;
using Omnikeeper.GridView.Request;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Commands
{
    public class AddContextCommand
    {
        public class Command : IRequest<bool>
        {
            public AddContextRequest Context { get; set; }
        }

        public class AddContextHandler : IRequestHandler<Command, bool>
        {
            private readonly IGridViewConfigModel gridViewConfigModel;
            public AddContextHandler(IGridViewConfigModel gridViewConfigModel)
            {
                this.gridViewConfigModel = gridViewConfigModel;
            }

            public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
            {
                var isSuccess = await gridViewConfigModel.AddContext(request.Context.Name, request.Context.Configuration);

                return isSuccess;
            }
        }
    }
}
