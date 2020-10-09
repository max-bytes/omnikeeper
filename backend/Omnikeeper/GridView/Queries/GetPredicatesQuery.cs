using LandscapeRegistry.GridView.Response;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LandscapeRegistry.GridView.Queries
{
    public class GetPredicatesQuery : IRequest<int>
    {
        //public class Query : IQueryable<int>
        //{
        //}
        public int PredicateId { get; set; }

        public class GetPredicatesQueryHandler : IRequestHandler<GetPredicatesQuery, int>
        {


            public async Task<int> Handle(GetPredicatesQuery request, CancellationToken cancellationToken)
            {
                //await Task.Delay(2000);

                return request.PredicateId;

            }
        }
    }
}
