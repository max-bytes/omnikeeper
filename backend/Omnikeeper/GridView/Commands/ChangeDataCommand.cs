using MediatR;
using Npgsql;
using Omnikeeper.GridView.Request;
using Omnikeeper.GridView.Response;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Commands
{
    public class ChangeDataCommand
    {
        public class Command : IRequest<ChangeDataResponse>
        {
            public ChangeDataRequest Changes { get; set; }
        }

        public class ChangeDataCommandHandler : IRequestHandler<Command, ChangeDataResponse>
        {
            private readonly NpgsqlConnection conn;
            public ChangeDataCommandHandler(NpgsqlConnection connection)
            {
                conn = connection;
            }
            public async Task<ChangeDataResponse> Handle(Command request, CancellationToken cancellationToken)
            {


                var result = await FetchData();
                return result;
            }

            private async Task<ChangeDataResponse> FetchData()
            {
                var result = new ChangeDataResponse();

                return result;
            }
        }
    }
}
