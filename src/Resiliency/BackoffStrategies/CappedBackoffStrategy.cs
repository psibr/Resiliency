using System;

namespace Resiliency.BackoffStrategies
{
    /// <summary>
    /// An <see cref="IBackoffStrategy"/> that restricts the wait time of an <see cref="IBackoffStrategy"/> to a given maximum value.
    /// </summary>
    public class CappedBackoffStrategy 
        : IBackoffStrategy
    {
        private readonly IBackoffStrategy _strategy;
        private readonly TimeSpan _maxWaitTime;

        public CappedBackoffStrategy(IBackoffStrategy strategy, TimeSpan maxWaitTime)
        {
            if (maxWaitTime < TimeSpan.Zero)
                throw new ArgumentException("The maximum wait time cannot be less than zero.", nameof(maxWaitTime));

            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _maxWaitTime = maxWaitTime;
        }

        public TimeSpan InitialWaitTime => _strategy.InitialWaitTime;

        public TimeSpan Next()
        {
            var minTicks = Math.Min(_maxWaitTime.Ticks, _strategy.Next().Ticks);

            return TimeSpan.FromTicks(minTicks);
        }
    }
}