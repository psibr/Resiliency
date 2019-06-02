using Resiliency.BackoffStrategies;
using System;

namespace Resiliency
{
    /// <summary>
    /// A set of factory methods that provide common base strategies for backoffs.
    /// </summary>
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
