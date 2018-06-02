using System;
using Xunit;
using Resiliency.BackoffStrategies;

namespace Resiliency.Tests.BackoffStrategies
{
    public class ExponentialBackoffTests
    {
        [Fact]
        public void WaitTimeIncreasesExponentially()
        {
            var initialWaitTime = TimeSpan.FromSeconds(30);
            var strategy = new ExponentialBackoffStrategy(initialWaitTime);

            var attempt1 = strategy.Next();
            var attempt2 = strategy.Next();
            var attempt3 = strategy.Next();
            var attempt4 = strategy.Next();

            Assert.Equal(initialWaitTime * Math.Pow(2, 0), attempt1);
            Assert.Equal(initialWaitTime * Math.Pow(2, 1), attempt2);
            Assert.Equal(initialWaitTime * Math.Pow(2, 2), attempt3);
            Assert.Equal(initialWaitTime * Math.Pow(2, 3), attempt4);
        }
    }
}