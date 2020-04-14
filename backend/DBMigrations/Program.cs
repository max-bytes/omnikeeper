using DbUp;
using System;
using System.Linq;
using System.Reflection;

namespace DBMigrations
{
    class Program
    {
        static int Main(string[] args)
        {
            // TODO
            var connectionString = args.FirstOrDefault() ?? "Server=host.docker.internal;User Id=postgres; Password=postgres;Database=landscape_prototype_test;Pooling=true";

            var result = DBMigration.Migrate(connectionString);

            if (!result.Successful)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(result.Error);
                Console.ResetColor();
                return -1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success!");
            Console.ResetColor();
            return 0;
        }
    }
}
