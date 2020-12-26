using SpeedrunComSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveSplit.AverageTime.Extensions
{
    public static class RunExtensions
    {
        private const long ONE_SECOND = TimeSpan.TicksPerMillisecond * 1000;

        public static TimeSpan GetAveragePrimaryTime(this IEnumerable<Run> runs) {
            var primaryTimes = runs
                .Take(Convert.ToInt32(runs.Count() * 0.95))
                .Select(run => run.Times.Primary?.Ticks ?? 0);
            var includesMilliseconds = primaryTimes.Any(time => time % ONE_SECOND != 0);

            var average = primaryTimes.Average();
            // Round down to the nearest second when miliseconds are not included
            if (!includesMilliseconds) {
                average -= average % ONE_SECOND;
            }

            // Convert double to long as the last step to avoid most rounding errors
            return new TimeSpan(Convert.ToInt64(average));
        }
    }
}
