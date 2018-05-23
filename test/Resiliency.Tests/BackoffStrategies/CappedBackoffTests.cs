using System;
using Xunit;
using Resiliency.BackoffStrategies;

namespace Resiliency.Tests.BackoffStrategies
{
    public class CappedBackoffTests
    {
        [Fact]
        public void WaitTimeUnderCapIsUnchanged()
        {
            var initialWaitTime = TimeSpan.FromSeconds(30);
            var cappedWaitTime = TimeSpan.FromSeconds(45);

            var cappedStrategy = 
                new ConstantBackoffStrategy(initialWaitTime)
                    .WithMaxWaitTime(cappedWaitTime);
                    
            var waitTime = cappedStrategy.GetWaitTime(1);

            Assert.True(waitTime < cappedWaitTime);
        }

        [Fact]
        public void WaitTimeIsCapped()
        {
            var initialWaitTime = TimeSpan.FromSeconds(30);
            var cappedWaitTime = TimeSpan.FromSeconds(15);

            var cappedStrategy = 
                new ConstantBackoffStrategy(initialWaitTime)
                    .WithMaxWaitTime(cappedWaitTime);
                    
            var waitTime = cappedStrategy.GetWaitTime(1);

            Assert.Equal(cappedWaitTime, waitTime);
        }
        
        [Fact]
        public void NegativeWaitTimeThrowsException()
        {
            var initialWaitTime = TimeSpan.FromSeconds(30);
            var cappedWaitTime = TimeSpan.FromSeconds(-15);

            Assert.Throws<ArgumentException>(() =>
                new ConstantBackoffStrategy(initialWaitTime)
                    .WithMaxWaitTime(cappedWaitTime));
        }

        [Fact]
        public void NullStrategyThrowsException()
        {
            IBackoffStrategy decoratedStrategy = null;

            Assert.Throws<ArgumentNullException>(() => new CappedBackoffStrategy(decoratedStrategy, TimeSpan.FromSeconds(1)));
        }
    }
}