using Resiliency.BackoffStrategies;
using System;

namespace Resiliency
{
    public static class Backoff
    {
        public static IBackoffStrategy Exponentially(TimeSpan initialWaitTime)
        {
            return new ExponentialBackoffStrategy(initialWaitTime);
        }

        public static IBackoffStrategy Linearlly(TimeSpan initialWaitTime)
        {
            return new LinearBackoffStrategy(initialWaitTime);
        }

        public static IBackoffStrategy Constantly(TimeSpan initialWaitTime)
        {
            return new ConstantBackoffStrategy(initialWaitTime);
        }
    }
}
