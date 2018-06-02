using System;
using System.ComponentModel;

namespace Resiliency.BackoffStrategies.Jitter
{
    /// <summary>
    /// Adds a randomized offset to the wait time of an <see cref="IBackoffStrategy"/> based on the underlying strategy's calculated wait time.
    /// <para/>
    /// Applies a simple random multiplier on the calculated wait time.
    /// </summary>
    public class MultiplicativeJitterBackoffStrategy 
        : IBackoffStrategy
    {
        private readonly IBackoffStrategy _strategy;
        private readonly IRandomNumberGenerator _randomNumberGenerator;

        public MultiplicativeJitterBackoffStrategy(IBackoffStrategy strategy, double minMultiplier, double maxMultiplier)
            : this(strategy, minMultiplier, maxMultiplier, new DefaultRandomNumberGenerator())
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public MultiplicativeJitterBackoffStrategy(IBackoffStrategy strategy, double minMultiplier, double maxMultiplier, IRandomNumberGenerator randomNumberGenerator = null)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            MinMultiplier = minMultiplier;
            MaxMultiplier = maxMultiplier;
            _randomNumberGenerator = randomNumberGenerator ?? throw new ArgumentNullException(nameof(randomNumberGenerator));
            if (minMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(minMultiplier), "Must be greater than or equal to 0");
            if (maxMultiplier <= 0) throw new ArgumentOutOfRangeException(nameof(maxMultiplier), "Must be greater than 0");
            if (minMultiplier > maxMultiplier) throw new ArgumentOutOfRangeException(nameof(maxMultiplier), $"Max multiplier should be greater than {nameof(minMultiplier)}");
        }

        public double MinMultiplier { get; }
        public double MaxMultiplier { get; }

        TimeSpan IBackoffStrategy.InitialWaitTime => _strategy.InitialWaitTime;

        public TimeSpan Next()
        {
            var waitTimeMs = _strategy.Next().TotalMilliseconds;
            waitTimeMs = waitTimeMs * _randomNumberGenerator.Next(MinMultiplier, MaxMultiplier);

            return TimeSpan.FromMilliseconds(waitTimeMs);
        }
    }
};