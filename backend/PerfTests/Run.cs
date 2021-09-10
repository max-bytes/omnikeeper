using BenchmarkDotNet.Running;

namespace PerfTests
{
    public class Run
    {
        static void Main(string[] args)
            => BenchmarkSwitcher.FromAssembly(typeof(Run).Assembly).Run(args);
    }
}
