using System;
using System.Collections.Generic;
using System.Text;

namespace Resiliency.BackoffStrategies
{
    public class PercentageAdjustedBackoffStrategy : IBackoffStrategy
    {
        private readonly IBackoffStrategy _strategy;
        private readonly double _percentageAdjustment;

        public PercentageAdjustedBackoffStrategy(IBackoffStrategy strategy, double percentageAdjustment)
        {
            // Prevent violations of causality. Current systems executing this code don't have this problem,
            // but who knows what system this will run on in the future.
            if (percentageAdjustment < 0)
                throw new ArgumentException("A negative percentage will result in a negative wait time and is not allowed.", nameof(percentageAdjustment));

            _strategy = strategy;
            _percentageAdjustment = percentageAdjustment;
        }

        public TimeSpan InitialWaitTime => _strategy.InitialWaitTime;

        public TimeSpan GetWaitTime(int attemptNumber)
        {
            var waitTimeMs = _strategy.GetWaitTime(attemptNumber).TotalMilliseconds * _percentageAdjustment;

            return TimeSpan.FromMilliseconds(waitTimeMs);
        }
    }

    public class ConstantAdjustedBackoffStrategy : IBackoffStrategy
    {
        private readonly IBackoffStrategy _strategy;
        private readonly TimeSpan _adjustment;

        public ConstantAdjustedBackoffStrategy(IBackoffStrategy strategy, TimeSpan adjustment)
        {
            if (adjustment + strategy.InitialWaitTime < TimeSpan.Zero)
                throw new ArgumentException("A negative adjustment cannot result in a wait time less than zero.", nameof(adjustment));

            _strategy = strategy;
            _adjustment = adjustment;
        }

        public TimeSpan InitialWaitTime => _strategy.InitialWaitTime;

        public TimeSpan GetWaitTime(int attemptNumber)
        {
            var adjustedWaitTime = _strategy.GetWaitTime(attemptNumber) + _adjustment;

            // Depending on the underlying strategy, it is still possible to have a negative wait time after adjustment.
            if (adjustedWaitTime < TimeSpan.Zero)
                adjustedWaitTime = TimeSpan.Zero;

            return adjustedWaitTime;
        }
    }
}
