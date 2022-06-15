using System;
using System.Diagnostics;

namespace Omnikeeper.Base.Utils
{
    public class StopTimer
    {
        private readonly Stopwatch stopWatch;

        public StopTimer()
        {
            stopWatch = new Stopwatch();
            stopWatch.Start();
        }

        public void Stop(Action<TimeSpan, string> onFinishedF)
        {
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTimeStr = string.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            onFinishedF(ts, elapsedTimeStr);
        }
    }
}
