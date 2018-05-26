using System;

namespace Resiliency.BackoffStrategies.Jitter
{
    /// <summary>
    /// Adds a randomized offset to the wait time of an <see cref="IBackoffStrategy"/> based on the given last wait time.
    /// <para/>
    /// Ranges from the wait time given by the underlying strategy up to 3 times the last given wait time.
    /// </summary>
    public class DecorrelatedJitterBackoffStrategy : IBackoffStrategy
    {
        private readonly IBackoffStrategy _strategy;
        private readonly TimeSpan _lastWaitTime;
        private readonly IRandomNumberGenerator _randomNumberGenerator;

        public DecorrelatedJitterBackoffStrategy(IBackoffStrategy strategy, TimeSpan lastWaitTime, IRandomNumberGenerator randomNumberGenerator)
        {
            if (lastWaitTime < TimeSpan.Zero)
                throw new ArgumentException("The last wait time cannot be less than zero.", nameof(lastWaitTime));

            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _lastWaitTime = lastWaitTime;
            _randomNumberGenerator = randomNumberGenerator ?? throw new ArgumentNullException(nameof(randomNumberGenerator));
        }

        TimeSpan IBackoffStrategy.InitialWaitTime => _strategy.InitialWaitTime;

        public TimeSpan GetWaitTime(int attemptNumber)
        {
            if (attemptNumber < 1)
                throw new ArgumentException($"The number of attempts cannot be less than 1 when getting the wait time of an {nameof(IBackoffStrategy)}.", nameof(attemptNumber));

            var waitTimeMs = 
                _randomNumberGenerator.NextDouble() * (_lastWaitTime.TotalMilliseconds * 3 - _strategy.InitialWaitTime.TotalMilliseconds) 
                + _strategy.InitialWaitTime.TotalMilliseconds;

            return TimeSpan.FromMilliseconds(waitTimeMs);
        }
    }
}