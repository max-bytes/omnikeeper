using System;

namespace Omnikeeper.Base.Utils
{
    public readonly record struct TimeThreshold(bool IsLatest, DateTimeOffset Time)
    {
        public static TimeThreshold BuildLatest()
        {
            return new TimeThreshold(true, DateTimeOffset.Now);
        }

        public static TimeThreshold BuildAtTime(DateTimeOffset time)
        {
            return new TimeThreshold(false, time);
        }

        public static TimeThreshold BuildLatestAtTime(DateTimeOffset time)
        {
            return new TimeThreshold(true, time);
        }

        public override string ToString()
        {
            return (IsLatest) ? "latest" : Time.ToString();
        }
    }
}
