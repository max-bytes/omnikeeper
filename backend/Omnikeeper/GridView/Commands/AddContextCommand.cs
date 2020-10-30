using MediatR;
using Newtonsoft.Json;
using Npgsql;
using Omnikeeper.Base.Utils;
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
            private readonly NpgsqlConnection conn;
            public AddContextHandler(NpgsqlConnection conn)
            {
                this.conn = conn;
            }

            public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
            {
                using var command = new NpgsqlCommand($@"
                    INSERT INTO gridview_config
                    (config, name, timestamp)
                    VALUES
                    (@config, @name, @timestamp)
                ", conn, null);

                var config = JsonConvert.SerializeObject(request.Context.Configuration);

                command.Parameters.AddWithValue("config", config);
                command.Parameters.AddWithValue("name", request.Context.Name);
                command.Parameters.AddWithValue("timestamp", TimeThreshold.BuildLatest());

                var result = await command.ExecuteNonQueryAsync();

                return result > 0;
            }
        }
    }
}
