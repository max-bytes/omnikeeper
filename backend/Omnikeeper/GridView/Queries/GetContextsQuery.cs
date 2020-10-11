using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Queries
{
    public class GetContextsQuery
    {
        public class Query : IRequest<int>
        {

        }

        public class GetContextsQueryHandler : IRequestHandler<GetContextsQuery.Query, int>
        {
            public Task<int> Handle(Query request, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
