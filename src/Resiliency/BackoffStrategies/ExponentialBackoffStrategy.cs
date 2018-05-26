using System;

namespace Resiliency.BackoffStrategies
{
    /// <summary>
    /// An <see cref="IBackoffStrategy"/> that returns a wait time that grows exponentialls with each attempt made.
    /// </summary>
    public class ExponentialBackoffStrategy : IBackoffStrategy
    {
        public ExponentialBackoffStrategy(TimeSpan initialWaitTime) 
        {
            if (initialWaitTime < TimeSpan.Zero)
                throw new ArgumentException("The initial wait time cannot be less than zero.", nameof(initialWaitTime));

            InitialWaitTime = initialWaitTime;
        }

        public TimeSpan InitialWaitTime { get; }

        public virtual TimeSpan GetWaitTime(int attemptNumber)
        {
            if (attemptNumber < 1)
                throw new ArgumentException($"The number of attempts cannot be less than 1 when getting the wait time of an {nameof(IBackoffStrategy)}.", nameof(attemptNumber));

            var waitTimeMs = InitialWaitTime.TotalMilliseconds * Math.Pow(2, attemptNumber - 1);
            return TimeSpan.FromMilliseconds(waitTimeMs);
        }
    }
}

