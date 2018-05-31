using System;

namespace Resiliency.BackoffStrategies
{
    /// <summary>
    /// An <see cref="IBackoffStrategy"/> that offsets an <see cref="IBackoffStrategy"/> by a percentage of the wait time.
    /// </summary>
    public class PercentageAdjustedBackoffStrategy 
        : IBackoffStrategy
    {
        private readonly IBackoffStrategy _strategy;
        private readonly double _percentageAdjustment;

        public PercentageAdjustedBackoffStrategy(IBackoffStrategy strategy, double percentageAdjustment)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

            // Prevent violations of causality. Current systems executing this code don't have this problem,
            // but who knows what system this will run on in the future.
            if (percentageAdjustment < 0)
                throw new ArgumentException("A negative percentage will result in a negative wait time and is not allowed.", nameof(percentageAdjustment));

            _percentageAdjustment = percentageAdjustment;
        }

        public TimeSpan InitialWaitTime => _strategy.InitialWaitTime;

        public TimeSpan Next()
        {
            var waitTimeMs = _strategy.Next().TotalMilliseconds * _percentageAdjustment;

            return TimeSpan.FromMilliseconds(waitTimeMs);
        }
    }
}
