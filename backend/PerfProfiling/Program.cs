using PerfTests;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PerfProfiling
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var p = new Profile();
            await p.Run();
        }
    }


    public class Profile : GetMergedAttributesTest
    {
        public async Task Run()
        {
            CIIDSelection = "specific";
            AttributeCITuple = AttributeCITuples.First();

            await Setup();

            Thread.Sleep(3000);

            await GetMergedAttributes();

            TearDown();
        }
    }

}
