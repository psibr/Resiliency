using System;
using Xunit;
using Resiliency.BackoffStrategies;

namespace Resiliency.Tests.BackoffStrategies
{
    public class LinearBackoffTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void WaitTimeIncreasesLinearly(int attempt)
        {
            var initialWaitTime = TimeSpan.FromSeconds(30);
            var strategy = new LinearBackoffStrategy(initialWaitTime);

            var waitTime = strategy.GetWaitTime(attempt);

            Assert.Equal(initialWaitTime * attempt, waitTime);
        }
    }
}