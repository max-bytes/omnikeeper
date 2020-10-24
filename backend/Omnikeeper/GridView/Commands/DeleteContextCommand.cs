using MediatR;
using Npgsql;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Commands
{
    public class DeleteContextCommand
    {
        public class Command : IRequest<bool>
        {
            public string Name { get; set; }
        }

        public class DeleteContextCommandHandler : IRequestHandler<Command, bool>
        {
            private readonly NpgsqlConnection conn;
            public DeleteContextCommandHandler(NpgsqlConnection conn)
            {
                this.conn = conn;
            }

            public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
            {
                using var command = new NpgsqlCommand($@"
                    DELETE FROM gridview_config
                    WHERE name = @name
                ", conn, null);

                command.Parameters.AddWithValue("name", request.Name);

                var result = await command.ExecuteNonQueryAsync();

                return result > 0;
            }
        }
    }
}
