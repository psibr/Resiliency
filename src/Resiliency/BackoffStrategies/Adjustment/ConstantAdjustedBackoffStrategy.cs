using System;

namespace Resiliency.BackoffStrategies
{
    /// <summary>
    /// An <see cref="IBackoffStrategy"/> that offsets an <see cref="IBackoffStrategy"/ by a constant factor.
    /// </summary>
    public class ConstantAdjustedBackoffStrategy 
        : IBackoffStrategy
    {
        private readonly IBackoffStrategy _strategy;
        private readonly TimeSpan _adjustment;

        public ConstantAdjustedBackoffStrategy(IBackoffStrategy strategy, TimeSpan adjustment)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

            if (adjustment + strategy.InitialWaitTime < TimeSpan.Zero)
                throw new ArgumentException("A negative adjustment cannot result in a wait time less than zero.", nameof(adjustment));

            _adjustment = adjustment;
        }

        public TimeSpan InitialWaitTime => _strategy.InitialWaitTime;

        public TimeSpan Next()
        {
            var adjustedWaitTime = _strategy.Next() + _adjustment;

            // Depending on the underlying strategy, it is still possible to have a negative wait time after adjustment.
            if (adjustedWaitTime < TimeSpan.Zero)
                adjustedWaitTime = TimeSpan.Zero;

            return adjustedWaitTime;
        }
    }
}
