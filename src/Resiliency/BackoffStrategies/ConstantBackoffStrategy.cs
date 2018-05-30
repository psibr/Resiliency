using System;

namespace Resiliency.BackoffStrategies
{
    /// <summary>
    /// An <see cref="IBackoffStrategy"/> that always returns the same wait time.
    /// </summary>
    public class ConstantBackoffStrategy 
        : IBackoffStrategy
    {
        public ConstantBackoffStrategy(TimeSpan waitTime)
        {
            if (waitTime < TimeSpan.Zero)
                throw new ArgumentException("The initial wait time cannot be less than zero.", nameof(waitTime));

            InitialWaitTime = waitTime;
        }

        public TimeSpan InitialWaitTime { get; }

        public TimeSpan Next()
        {
            return InitialWaitTime;
        }
    }
}