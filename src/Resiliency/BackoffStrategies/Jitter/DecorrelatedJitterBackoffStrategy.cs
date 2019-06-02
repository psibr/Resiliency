using System;

namespace Resiliency.BackoffStrategies.Jitter
{
    /// <summary>
    /// Adds a randomized offset to the wait time of an <see cref="IBackoffStrategy"/> based on the given last wait time.
    /// <para/>
    /// Ranges from the wait time given by the underlying strategy up to 3 times the last given wait time.
    /// </summary>
    public class DecorrelatedJitterBackoffStrategy 
        : IBackoffStrategy
        , IRequireRandom
    {
        private readonly IBackoffStrategy _strategy;

        private TimeSpan _lastWaitTime;

        public DecorrelatedJitterBackoffStrategy(IBackoffStrategy strategy)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _lastWaitTime = _strategy.InitialWaitTime;
        }

        TimeSpan IBackoffStrategy.InitialWaitTime => _strategy.InitialWaitTime;

        public IRandomNumberGenerator RandomNumberGenerator { get; set; } = new DefaultRandomNumberGenerator();

        public TimeSpan Next()
        {
            var waitTime = TimeSpan.FromMilliseconds(
                RandomNumberGenerator.Next(
                    minValue: _strategy.Next().TotalMilliseconds, 
                    maxValue: _lastWaitTime.TotalMilliseconds * 3));

            _lastWaitTime = waitTime;

            return waitTime;
        }
    }
}