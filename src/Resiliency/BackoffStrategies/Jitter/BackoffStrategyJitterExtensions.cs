using System;
using System.Collections.Generic;
using System.Text;

namespace Resiliency.BackoffStrategies.Jitter
{
    public static class BackoffStrategyJitterExtensions
    {
        public static IBackoffStrategy WithFullJitter(this IBackoffStrategy strategy, IRandomNumberGenerator randomNumberGenerator = null)
        {
            randomNumberGenerator = randomNumberGenerator ?? new DefaultRandomNumberGenerator();
            return new FullJitterBackoffStrategy(strategy, randomNumberGenerator);
        }

        public static IBackoffStrategy WithEqualJitter(this IBackoffStrategy strategy, IRandomNumberGenerator randomNumberGenerator = null)
        {
            randomNumberGenerator = randomNumberGenerator ?? new DefaultRandomNumberGenerator();
            return new HalfJitterBackoffStrategy(strategy, randomNumberGenerator);
        }

        public static IBackoffStrategy WithDecorrelatedJitter(this IBackoffStrategy strategy, TimeSpan lastWaitTime, IRandomNumberGenerator randomNumberGenerator = null)
        {
            randomNumberGenerator = randomNumberGenerator ?? new DefaultRandomNumberGenerator();
            return new DecorrelatedJitterBackoffStrategy(strategy, lastWaitTime, randomNumberGenerator);
        }
    }
}
