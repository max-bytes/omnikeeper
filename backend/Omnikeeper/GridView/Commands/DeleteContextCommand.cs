using MediatR;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Model;
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

            public Command(string Name)
            {
                this.Name = Name;
            }
        }

        public class DeleteContextCommandHandler : IRequestHandler<Command, Exception?>
        {
            private readonly IGridViewContextModel gridViewContextModel;
            private readonly IModelContextBuilder modelContextBuilder;
            public DeleteContextCommandHandler(IGridViewContextModel gridViewContextModel, IModelContextBuilder modelContextBuilder)
            {
                this.gridViewContextModel = gridViewContextModel;
                this.modelContextBuilder = modelContextBuilder;
            }

            public async Task<Exception?> Handle(Command request, CancellationToken cancellationToken)
            {
                using var trans = modelContextBuilder.BuildDeferred();

                try
                {
                    var isSuccess = await gridViewContextModel.DeleteContext(request.Name, trans);

                    if (isSuccess)
                    {
                        trans.Commit();
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    return ex;
                }

                return new Exception("An error occured deleting context");
            }
        }
    }
}
