using System;
using System.Linq;
using System.Threading;

namespace DBMigrations
{
    class Program
    {
        static int Main(string[] args)
        {
            // TODO
            var connectionString = args.FirstOrDefault();

            if (connectionString == null)
                throw new Exception("No connection string provided");

            var numRetries = 3;
            var succeeded = false;
            do
            {
                var result = DBMigration.Migrate(connectionString);

                if (!result.Successful)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(result.Error);
                    Console.ResetColor();

                    Thread.Sleep(TimeSpan.FromSeconds(5).Milliseconds);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Success!");
                    Console.ResetColor();
                    succeeded = true;
                }
            } while (numRetries > 0 && !succeeded);

            if (!succeeded) return -1;
            return 0;
        }
    }
}
