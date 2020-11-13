using MediatR;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Response;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Queries
{
    public class GetContextsQuery
    {
        public class Query : IRequest<GetContextsResponse>
        {

        }

        public class GetContextsQueryHandler : IRequestHandler<Query, GetContextsResponse>
        {
            private readonly IGridViewConfigModel gridViewConfigModel;
            private readonly IModelContextBuilder modelContextBuilder;
            public GetContextsQueryHandler(IGridViewConfigModel gridViewConfigModel, IModelContextBuilder modelContextBuilder)
            {
                this.gridViewConfigModel = gridViewConfigModel;
                this.modelContextBuilder = modelContextBuilder;
            }

            async Task<GetContextsResponse> IRequestHandler<Query, GetContextsResponse>.Handle(Query request, CancellationToken cancellationToken)
            {
                var result = new GetContextsResponse
                {
                    Contexts = await gridViewConfigModel.GetContexts(modelContextBuilder.BuildImmediate())
                };

                return result;
            }
        }
    }
}
