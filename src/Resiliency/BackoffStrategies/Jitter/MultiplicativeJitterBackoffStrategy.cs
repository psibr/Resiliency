using System;

namespace Resiliency.BackoffStrategies.Jitter
{
    /// <summary>
    /// Adds a randomized offset to the wait time of an <see cref="IBackoffStrategy"/> based on the underlying strategy's calculated wait time.
    /// <para/>
    /// Applies a simple random multiplier on the calculated wait time.
    /// </summary>
    public class MultiplicativeJitterBackoffStrategy 
        : IBackoffStrategy
        , IRequireRandom
    {
        private readonly IBackoffStrategy _strategy;

        public MultiplicativeJitterBackoffStrategy(IBackoffStrategy strategy, double minMultiplier, double maxMultiplier)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            MinMultiplier = minMultiplier;
            MaxMultiplier = maxMultiplier;
            if (minMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(minMultiplier), "Must be greater than or equal to 0");
            if (maxMultiplier <= 0) throw new ArgumentOutOfRangeException(nameof(maxMultiplier), "Must be greater than 0");
            if (minMultiplier > maxMultiplier) throw new ArgumentOutOfRangeException(nameof(maxMultiplier), $"Max multiplier should be greater than {nameof(minMultiplier)}");
        }

        public double MinMultiplier { get; }
        public double MaxMultiplier { get; }

        public IRandomNumberGenerator RandomNumberGenerator { get; set; } = new DefaultRandomNumberGenerator();

        TimeSpan IBackoffStrategy.InitialWaitTime => _strategy.InitialWaitTime;

        public TimeSpan Next()
        {
            var waitTimeMs = _strategy.Next().TotalMilliseconds;
            waitTimeMs *= RandomNumberGenerator.Next(MinMultiplier, MaxMultiplier);

            return TimeSpan.FromMilliseconds(waitTimeMs);
        }
    }
}
