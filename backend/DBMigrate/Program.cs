using System;
using System.Linq;
using System.Threading;

namespace DBMigrations
{
    class Program
    {
        static int Main(string[] args)
        {
            var connectionString = args.FirstOrDefault();

            if (connectionString == null)
                throw new Exception("No connection string provided");

            var numRetries = 10;
            var succeeded = false;
            do
            {
                try
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
                } catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.Message);
                    Console.ResetColor();
                    Thread.Sleep(TimeSpan.FromSeconds(5).Milliseconds);
                }
                numRetries--;
            } while (numRetries > 0 && !succeeded);

            if (!succeeded) return -1;
            return 0;
        }
    }
}
