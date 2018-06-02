using System;
using Xunit;
using Resiliency.BackoffStrategies;

namespace Resiliency.Tests.BackoffStrategies
{
    public class LinearBackoffTests
    {
        [Fact]
        public void WaitTimeIncreasesLinearly()
        {
            var initialWaitTime = TimeSpan.FromSeconds(30);
            var strategy = new LinearBackoffStrategy(initialWaitTime);

            var attempt1 = strategy.Next();
            var attempt2 = strategy.Next();
            var attempt3 = strategy.Next();
            var attempt4 = strategy.Next();

            Assert.Equal(initialWaitTime, attempt1);
            Assert.Equal(initialWaitTime * 2, attempt2);
            Assert.Equal(initialWaitTime * 3, attempt3);
            Assert.Equal(initialWaitTime * 4, attempt4);
        }
    }
}