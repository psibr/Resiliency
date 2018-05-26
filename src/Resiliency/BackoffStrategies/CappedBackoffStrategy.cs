using System;

namespace Resiliency.BackoffStrategies
{
    /// <summary>
    /// An <see cref="IBackoffStrategy"/> that restricts the wait time of an <see cref="IBackoffStrategy"/> to a given maximum value.
    /// </summary>
    public class CappedBackoffStrategy : IBackoffStrategy
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

        public TimeSpan GetWaitTime(int attemptNumber)
        {
            if (attemptNumber < 1)
                throw new ArgumentException($"The number of attempts cannot be less than 1 when getting the wait time of an {nameof(IBackoffStrategy)}.", nameof(attemptNumber));

            var minTicks = Math.Min(_maxWaitTime.Ticks, _strategy.GetWaitTime(attemptNumber).Ticks);
            return TimeSpan.FromTicks(minTicks);
        }
    }
}