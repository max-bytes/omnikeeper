using MediatR;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Model;
using Omnikeeper.GridView.Response;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Queries
{
    public class GetContextQuery
    {
        public class Query : IRequest<(GetContextResponse, Exception?)>
        {
            public string ContextName { get; set; }
        }

        public class GetContextQueryHandler : IRequestHandler<Query, (GetContextResponse, Exception?)>
        {
            private readonly IGridViewContextModel gridViewContextModel;
            private readonly IModelContextBuilder modelContextBuilder;
            public GetContextQueryHandler(IGridViewContextModel gridViewContextModel, IModelContextBuilder modelContextBuilder)
            {
                this.gridViewContextModel = gridViewContextModel;
                this.modelContextBuilder = modelContextBuilder;
            }

            async Task<(GetContextResponse, Exception?)> IRequestHandler<Query, (GetContextResponse, Exception?)>.Handle(Query request, CancellationToken cancellationToken)
            {
                try
                {
                    var result = new GetContextResponse
                    {
                        Context = await gridViewContextModel.GetFullContextByName(request.ContextName, modelContextBuilder.BuildImmediate())
                    };

                    return (result, null);
                } catch (Exception e)
                {
                    return (new GetContextResponse(), e);
                }
            }
        }
    }
}
