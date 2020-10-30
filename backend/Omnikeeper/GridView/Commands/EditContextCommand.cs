using MediatR;
using Newtonsoft.Json;
using Npgsql;
using Omnikeeper.Base.Entity.GridView;
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
            private readonly NpgsqlConnection conn;
            public EditContextCommandHandler(NpgsqlConnection conn)
            {
                this.conn = conn;
            }

            public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
            {
                using var command = new NpgsqlCommand($@"
                    UPDATE gridview_config
                    SET config = @config
                    WHERE name = @name
                ", conn, null);

                var config = JsonConvert.SerializeObject(request.Configuration);

                command.Parameters.AddWithValue("config", config);
                command.Parameters.AddWithValue("name", request.Name);

                var result = await command.ExecuteNonQueryAsync();

                return result > 0;
            }
        }
    }
}
