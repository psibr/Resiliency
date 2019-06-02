using Resiliency.BackoffStrategies;
using Resiliency.BackoffStrategies.Jitter;

namespace Resiliency
{
    public static class BackoffStrategyJitterExtensions
    {
        /// <summary>
        /// Applies a randomness (jitter) algorithm that selects a value between 0 and the base wait time.
        /// </summary>
        /// <param name="strategy">The base wait strategy to apply over.</param>
        /// <returns>A backoff strategy composed of the base and full jitter.</returns>
        public static IBackoffStrategy WithFullJitter(this IBackoffStrategy strategy)
        {
            return new MultiplicativeJitterBackoffStrategy(strategy, 0, 1);
        }
        /// <summary>
        /// Applies a randomness (jitter) algorithm that selects a value between half of the base wait time and the full base wait time.
        /// </summary>
        /// <param name="strategy">The base wait strategy to apply over.</param>
        /// <returns>A backoff strategy composed of the base and equal jitter.</returns>
        public static IBackoffStrategy WithEqualJitter(this IBackoffStrategy strategy)
        {
            return new MultiplicativeJitterBackoffStrategy(strategy, .5, 1);
        }

        /// <summary>
        /// Applies a randomness (jitter) algorithm that ranges from the base wait time up to 3 times the last wait time.
        /// </summary>
        /// <param name="strategy">The base wait strategy to apply over.</param>
        /// <returns>A backoff strategy composed of the base and equal jitter.</returns>
        public static IBackoffStrategy WithDecorrelatedJitter(this IBackoffStrategy strategy)
        {
            return new DecorrelatedJitterBackoffStrategy(strategy);
        }
    }
}
