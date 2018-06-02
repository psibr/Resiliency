using System;

namespace Resiliency.BackoffStrategies
{
    /// <summary>
    /// An <see cref="IBackoffStrategy"/> that returns a wait time that increases linearly with each attempt made.
    /// </summary>
    public class LinearBackoffStrategy 
        : IBackoffStrategy
    {
        private int _attemptNumber = 0;

        public LinearBackoffStrategy(TimeSpan initialWaitTime)
        {
            if (initialWaitTime < TimeSpan.Zero)
                throw new ArgumentException("The initial wait time cannot be less than zero.", nameof(initialWaitTime));

            InitialWaitTime = initialWaitTime;
        }

        public TimeSpan InitialWaitTime { get; }

        public TimeSpan Next()
        {
            return TimeSpan.FromTicks(InitialWaitTime.Ticks * ++_attemptNumber);
        }
    }
}