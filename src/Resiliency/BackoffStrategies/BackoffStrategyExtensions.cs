using Resiliency.BackoffStrategies;
using System;

namespace Resiliency
{
    public static class BackoffStrategyExtensions
    {
        public static IBackoffStrategy WithMaxWaitTime(this IBackoffStrategy strategy, TimeSpan maxWaitTime)
        {
            return new CappedBackoffStrategy(strategy, maxWaitTime);
        }
        
        public static IBackoffStrategy WithAdjustment(this IBackoffStrategy strategy, double percentageAdjustment)
        {
            return new PercentageAdjustedBackoffStrategy(strategy, percentageAdjustment);
        }

        public static IBackoffStrategy WithAdjustment(this IBackoffStrategy strategy, TimeSpan adjustment)
        {
            return new ConstantAdjustedBackoffStrategy(strategy, adjustment);
        }
    }
}