using BenchmarkDotNet.Running;
using System.Threading.Tasks;

namespace PerfTests
{
    public class Run
    {
        static void Main(string[] args)
            => BenchmarkSwitcher.FromAssembly(typeof(Run).Assembly).Run(args);
    }
    //public class Run
    //{
    //    static async Task Main(string[] args)
    //    {
    //        var tmp = new GetTraitEntitiesByCIIDTest();
    //        await tmp.RunDebuggable();
    //    }
    //}
}
