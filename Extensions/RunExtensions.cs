using SpeedrunComSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveSplit.AverageTime.Extensions
{
    public static class RunExtensions
    {
        private const long ONE_SECOND = TimeSpan.TicksPerMillisecond * 1000;
        private const long TIME_BONUS_DIVISOR = 3600 * 12 * ONE_SECOND; // 12h (1/2 day) for +100%

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

            // Convert double to long as the last step to avoid rounding errors
            return new TimeSpan(Convert.ToInt64(average));
        }

        public static TimeSpan GetScoreDropTime(this IEnumerable<Run> runs) {
            var primaryTimes = runs
                .Take(Convert.ToInt32(runs.Count() * 0.95))
                .Select(run => run.Times.Primary?.Ticks ?? 0);

            var m = (double)primaryTimes.Average();
            var t = (double)primaryTimes.First();
            var w = (double)primaryTimes.Last();
            var N = (double)primaryTimes.Count();

            // Original algorithm (https://github.com/Avasam/speedrun.com_global_scoreboard_webapp/blob/master/README.md)
            // (e ^ (Min[pi, (w - t) / (w - m)] * (1 - 1 / (N - 1))) - 1) * 10 * (1 + (t / 43200)) = p; N = <population>; t = <time>; w = <worst time>; m = <mean>
            double p = (Math.Exp(Math.Min(Math.PI, (w - t) / (w - m)) * (1 - 1 / (N - 1))) - 1) * 10 * (1 + (t / TIME_BONUS_DIVISOR));
            p = Math.Floor(p);
            
            // Looking for the mean (x) with added run when we know the score
            // (e ^ ((w - t) / (w - x) * (1 - 1 / N)) - 1) * 10 * (1 + (t / 43200)) < p; N = <original population>; t = <time>; w = <worst time>; p = <final  score>
            // when solving for x, becomes
            // -((w-t) / (Log(p / (1 + (t / 43200)) / 10 + 1) / (1-1/N)) - w) < x
            var x = -((w - t) / (Math.Log(p / (1 + (t / TIME_BONUS_DIVISOR)) / 10 + 1) / (1 - 1 / N)) - w);

            // Find the required time (n)
            // (m * N + n) / (N + 1) = x; N = <original population>; m = <mean>; x = <targetted mean>
            // when solving for n, becomes
            // x * (N + 1) - m * N = n
            var n = x * (N + 1) - m * N;

            // Round down to the nearest second
            n -= n % ONE_SECOND;

            // Convert double to long as the last step to avoid rounding errors
            return new TimeSpan(Convert.ToInt64(n));
        }
    }
}
