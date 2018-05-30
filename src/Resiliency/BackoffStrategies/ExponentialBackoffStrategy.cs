using System;

namespace Resiliency.BackoffStrategies
{
    /// <summary>
    /// An <see cref="IBackoffStrategy"/> that returns a wait time that grows exponentially with each attempt made.
    /// </summary>
    public class ExponentialBackoffStrategy 
        : IBackoffStrategy
    {
        private int _attemptNumber;

        public ExponentialBackoffStrategy(TimeSpan initialWaitTime) 
        {
            if (initialWaitTime < TimeSpan.Zero)
                throw new ArgumentException("The initial wait time cannot be less than zero.", nameof(initialWaitTime));

            InitialWaitTime = initialWaitTime;
        }

        public TimeSpan InitialWaitTime { get; }

        public virtual TimeSpan Next()
        {
            var waitTimeMs = InitialWaitTime.TotalMilliseconds * Math.Pow(2, ++_attemptNumber - 1);

            return TimeSpan.FromMilliseconds(waitTimeMs);
        }
    }
}