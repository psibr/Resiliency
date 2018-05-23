using System;

namespace Resiliency.BackoffStrategies.Jitter
{
    public class HalfJitterBackoffStrategy : IBackoffStrategy
    {
        private readonly IBackoffStrategy _strategy;
        private readonly IRandomNumberGenerator _randomNumberGenerator;

        public HalfJitterBackoffStrategy(IBackoffStrategy strategy, IRandomNumberGenerator randomNumberGenerator)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _randomNumberGenerator = randomNumberGenerator ?? throw new ArgumentNullException(nameof(randomNumberGenerator));
        }

        TimeSpan IBackoffStrategy.InitialWaitTime => _strategy.InitialWaitTime;

        public TimeSpan GetWaitTime(int attemptNumber)
        {
            if (attemptNumber < 1)
                throw new ArgumentException($"The number of attempts cannot be less than 1 when getting the wait time of an {nameof(IBackoffStrategy)}.", nameof(attemptNumber));

            var waitTimeMs = _strategy.GetWaitTime(attemptNumber).TotalMilliseconds;
            waitTimeMs = _randomNumberGenerator.NextDouble() * waitTimeMs / 2 + waitTimeMs;

            return TimeSpan.FromMilliseconds(waitTimeMs);
        }
    }
};