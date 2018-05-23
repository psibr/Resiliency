using System;

namespace Resiliency.BackoffStrategies
{
    public class LinearBackoffStrategy : IBackoffStrategy
    {
        public LinearBackoffStrategy(TimeSpan initialWaitTime)
        {
            if (initialWaitTime < TimeSpan.Zero)
                throw new ArgumentException("The initial wait time cannot be less than zero.", nameof(initialWaitTime));

            InitialWaitTime = initialWaitTime;
        }

        public TimeSpan InitialWaitTime { get; }

        public TimeSpan GetWaitTime(int attemptNumber)
        {
            if (attemptNumber < 1)
                throw new ArgumentException($"The number of attempts cannot be less than 1 when getting the wait time of an {nameof(IBackoffStrategy)}.", nameof(attemptNumber));

            return TimeSpan.FromTicks(InitialWaitTime.Ticks * attemptNumber);
        }
    }
}