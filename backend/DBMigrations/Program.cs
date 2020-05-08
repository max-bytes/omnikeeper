//using System;
//using System.Linq;

//namespace DBMigrations
//{
//    class Program
//    {
//        static int Main(string[] args)
//        {
//            // TODO
//            var connectionString = args.FirstOrDefault();

//            if (connectionString == null)
//                throw new Exception("No connection string provided");

//            var result = DBMigration.Migrate(connectionString);

//            if (!result.Successful)
//            {
//                Console.ForegroundColor = ConsoleColor.Red;
//                Console.WriteLine(result.Error);
//                Console.ResetColor();
//                return -1;
//            }

//            Console.ForegroundColor = ConsoleColor.Green;
//            Console.WriteLine("Success!");
//            Console.ResetColor();
//            return 0;
//        }
//    }
//}
