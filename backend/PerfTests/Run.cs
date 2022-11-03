using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System.Threading.Tasks;

namespace PerfTests
{
    public class Run
    {
        static void Main(string[] args)
            => BenchmarkSwitcher.FromAssembly(typeof(Run).Assembly).Run(args, 
                ManualConfig.Create(DefaultConfig.Instance).WithOptions(ConfigOptions.DisableOptimizationsValidator) // NOTE: necessary because of interaction with NUnit, see https://github.com/nunit/nunit/issues/3878
            );
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
