using System;
using System.Collections.Generic;
using System.Linq;

namespace Resiliency.BackoffStrategies
{
    /// <summary>
    /// An <see cref="IBackoffStrategy"/> that runs multiple <see cref="IBackoffStrategy"/>s and uses a given selector to choose a resulting wait time.
    /// </summary>
    public class CompositeBackoffStrategy : IBackoffStrategy
    {
        public IEnumerable<IBackoffStrategy> _strategies;
        private readonly Func<IEnumerable<TimeSpan>, TimeSpan> _waitTimeSelector;

        /// <summary>
        /// Runs multiple strategies and uses a selector function to choose the desired result.
        /// </summary>
        public CompositeBackoffStrategy(IBackoffStrategy[] strategies, Func<IEnumerable<TimeSpan>, TimeSpan> waitTimeSelector)
        {
            if (strategies == null)
                throw new ArgumentNullException(nameof(strategies));

            if (waitTimeSelector == null)
                throw new ArgumentNullException(nameof(waitTimeSelector));

            if (strategies.Length == 0)
                throw new ArgumentException($"A {nameof(CompositeBackoffStrategy)} must have at least one strategy provided.", nameof(strategies));

            _strategies = strategies;
            _waitTimeSelector = waitTimeSelector;
        }

        public TimeSpan InitialWaitTime => _waitTimeSelector(_strategies.Select(strategy => strategy.InitialWaitTime));

        public TimeSpan GetWaitTime(int attemptNumber)
        {
            if (attemptNumber < 1)
                throw new ArgumentException($"The number of attempts cannot be less than 1 when getting the wait time of an {nameof(IBackoffStrategy)}.", nameof(attemptNumber));

            return _waitTimeSelector(_strategies.Select(strategy => strategy.GetWaitTime(attemptNumber)));
        }
    }
}