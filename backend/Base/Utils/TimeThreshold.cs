using System;
using System.Collections.Generic;
using System.Text;

namespace Landscape.Base.Utils
{
    public struct TimeThreshold
    {
        private TimeThreshold(bool isLatest, DateTimeOffset time) { 
            IsLatest = isLatest;
            Time = time;
        }

        public bool IsLatest { get; }
        public DateTimeOffset Time { get; }

        public static TimeThreshold BuildLatest()
        {
            return new TimeThreshold(true, DateTimeOffset.Now);
        }

        public static TimeThreshold BuildAtTime(DateTimeOffset time)
        {
            return new TimeThreshold(false, time);
        }
    }
}
