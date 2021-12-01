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
        bool SETUP_DATA = false;
        public async Task Run()
        {
            CIIDSelection = "specific";
            UseLatestTable = true;
            AttributeCITuple = AttributeCITuples.First();
            PreSetupData = SETUP_DATA;

            await Setup();

            Thread.Sleep(3000);

            await GetMergedAttributes();

            TearDown();
        }
    }

}
