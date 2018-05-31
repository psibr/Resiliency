using System;
using System.ComponentModel;

namespace Resiliency.BackoffStrategies.Jitter
{
    /// <summary>
    /// Adds a randomized offset to the wait time of an <see cref="IBackoffStrategy"/> based on the given last wait time.
    /// <para/>
    /// Ranges from the wait time given by the underlying strategy up to 3 times the last given wait time.
    /// </summary>
    public class DecorrelatedJitterBackoffStrategy 
        : IBackoffStrategy
    {
        private readonly IBackoffStrategy _strategy;
        private readonly IRandomNumberGenerator _randomNumberGenerator;

        private TimeSpan _lastWaitTime;

        public DecorrelatedJitterBackoffStrategy(IBackoffStrategy strategy)
            : this(strategy, new DefaultRandomNumberGenerator())
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public DecorrelatedJitterBackoffStrategy(IBackoffStrategy strategy, IRandomNumberGenerator randomNumberGenerator)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _lastWaitTime = _strategy.InitialWaitTime;
            _randomNumberGenerator = randomNumberGenerator ?? throw new ArgumentNullException(nameof(randomNumberGenerator));
        }

        TimeSpan IBackoffStrategy.InitialWaitTime => _strategy.InitialWaitTime;

        public TimeSpan Next()
        {
            var waitTime = TimeSpan.FromMilliseconds(
                _randomNumberGenerator.Next(
                    minValue: _strategy.Next().TotalMilliseconds, 
                    maxValue: _lastWaitTime.TotalMilliseconds * 3));

            _lastWaitTime = waitTime;

            return waitTime;
        }
    }
}