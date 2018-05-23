using System;
using Xunit;
using Resiliency.BackoffStrategies;

namespace Resiliency.Tests.BackoffStrategies
{
    public class ExponentialBackoffTests
    {
        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 1)]
        [InlineData(3, 2)]
        [InlineData(4, 3)]
        public void WaitTimeIncreasesExponentially(int attempt, int exponent)
        {
            var initialWaitTime = TimeSpan.FromSeconds(30);
            var strategy = new ExponentialBackoffStrategy(initialWaitTime);

            var waitTime = strategy.GetWaitTime(attempt);

            Assert.Equal(initialWaitTime * Math.Pow(2, exponent), waitTime);
        }
    }
}