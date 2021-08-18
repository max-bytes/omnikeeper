using MediatR;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Model;
using Omnikeeper.GridView.Response;
using System.Linq;
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
            private readonly IGridViewContextModel gridViewContextModel;
            private readonly IModelContextBuilder modelContextBuilder;
            public GetContextsQueryHandler(IGridViewContextModel gridViewContextModel, IModelContextBuilder modelContextBuilder)
            {
                this.gridViewContextModel = gridViewContextModel;
                this.modelContextBuilder = modelContextBuilder;
            }

            async Task<GetContextsResponse> IRequestHandler<Query, GetContextsResponse>.Handle(Query request, CancellationToken cancellationToken)
            {
                var contexts = await gridViewContextModel.GetContexts(TimeThreshold.BuildLatest(), modelContextBuilder.BuildImmediate());

                return new GetContextsResponse(contexts.Values.ToList()); // TODO: why not return dictionary?
            }
        }
    }
}
