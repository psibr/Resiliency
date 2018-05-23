using System;

namespace Resiliency.BackoffStrategies
{
    public class ConstantBackoffStrategy : IBackoffStrategy
    {
        public ConstantBackoffStrategy(TimeSpan initialWaitTime)
        {
            if (initialWaitTime < TimeSpan.Zero)
                throw new ArgumentException("The initial wait time cannot be less than zero.", nameof(initialWaitTime));

            InitialWaitTime = initialWaitTime;
        }

        public TimeSpan InitialWaitTime { get; }

        public TimeSpan GetWaitTime(int attemptNumber)
        {
            return InitialWaitTime;
        }
    }
}