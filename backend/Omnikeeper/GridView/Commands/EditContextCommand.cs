using MediatR;
using Omnikeeper.Base.Entity.GridView;
using Omnikeeper.Base.Model;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Commands
{
    public class EditContextCommand
    {
        public class Command : IRequest<bool>
        {
            public string Name { get; set; }
            public GridViewConfiguration Configuration { get; set; }
        }

        public class EditContextCommandHandler : IRequestHandler<Command, bool>
        {
            private readonly IGridViewConfigModel gridViewConfigModel;
            public EditContextCommandHandler(IGridViewConfigModel gridViewConfigModel)
            {
                this.gridViewConfigModel = gridViewConfigModel;
            }

            public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
            {
                var isSuccess = await gridViewConfigModel.EditContext(request.Name, request.Configuration);

                return isSuccess;
            }
        }
    }
}
