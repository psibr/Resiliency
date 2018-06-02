using Resiliency.BackoffStrategies;
using System;

namespace Resiliency
{
    public static class Backoff
    {
        public static IBackoffStrategy ExponentiallyFrom(TimeSpan initialWaitTime)
        {
            return new ExponentialBackoffStrategy(initialWaitTime);
        }

        public static IBackoffStrategy LinearlyFrom(TimeSpan initialWaitTime)
        {
            return new LinearBackoffStrategy(initialWaitTime);
        }

        public static IBackoffStrategy Constant(TimeSpan waitTime)
        {
            return new ConstantBackoffStrategy(waitTime);
        }
    }
}
