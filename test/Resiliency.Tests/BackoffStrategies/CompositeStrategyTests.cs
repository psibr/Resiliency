using System;
using System.Linq;
using Xunit;
using Resiliency.BackoffStrategies;

namespace Resiliency.Tests.BackoffStrategies
{
    public class CompositeStrategyTests
    {
        private TimeSpan[] _initialWaitTimes;
        private ConstantBackoffStrategy[] _strategies;

        public CompositeStrategyTests()
        {
            _initialWaitTimes = new []
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(60)
            };

            _strategies = _initialWaitTimes.Select(initialWaitTime => new ConstantBackoffStrategy(initialWaitTime)).ToArray();
        }

        [Fact]
        public void CompositeWithMaxSelectorReturnsMaxWaitTime()
        {
            var strategy = new CompositeBackoffStrategy(_strategies, waitTimes => waitTimes.Max());
            var waitTime = strategy.GetWaitTime(1);

            Assert.Equal(_initialWaitTimes.Max(), waitTime);
        }

        [Fact]
        public void CompositeWithMinSelectorReturnsMinWaitTime()
        {
            var strategy = new CompositeBackoffStrategy(_strategies, waitTimes => waitTimes.Min());
            var waitTime = strategy.GetWaitTime(1);

            Assert.Equal(_initialWaitTimes.Min(), waitTime);
        }

        [Fact]
        public void CompositeWithAverageSelectorReturnsAverageWaitTime()
        {
            var strategy = new CompositeBackoffStrategy(_strategies, waitTimes => TimeSpan.FromMilliseconds(waitTimes.Average(time => time.TotalMilliseconds)));
            var waitTime = strategy.GetWaitTime(1);

            Assert.Equal(TimeSpan.FromMilliseconds(_initialWaitTimes.Average(time => time.TotalMilliseconds)), waitTime);
        }

        [Fact]
        public void CompositeSelectorReturnsCompletelyDifferentWaitTime()
        {
            var completelyDifferentWaitTime = TimeSpan.FromSeconds(23);
            var strategy = new CompositeBackoffStrategy(_strategies, waitTimes => completelyDifferentWaitTime);
            var waitTime = strategy.GetWaitTime(1);

            Assert.Equal(completelyDifferentWaitTime, waitTime);
        }

        [Fact]
        public void NullStrategyThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new CompositeBackoffStrategy(null, times => times.First()));
        }
    }
}