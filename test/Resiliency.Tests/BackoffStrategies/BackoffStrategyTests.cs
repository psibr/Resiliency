using System;
using Xunit;
using Resiliency.BackoffStrategies;
using Resiliency.BackoffStrategies.Jitter;

namespace Resiliency.Tests.BackoffStrategies
{
    public class BackoffStrategyTests
    {
        [Theory]
        [InlineData(typeof(ConstantBackoffStrategy))]
        [InlineData(typeof(LinearBackoffStrategy))]
        [InlineData(typeof(ExponentialBackoffStrategy))]
        public void NegativeInitialValueThrowsException(Type strategyType)
        {
            TimeSpan initialWaitTime = TimeSpan.FromSeconds(-30);
            Assert.Throws<ArgumentException>(() =>
            {
                try
                {
                    Activator.CreateInstance(strategyType, new object[] { initialWaitTime });
                }
                catch (System.Reflection.TargetInvocationException tiex)
                {
                    throw tiex.InnerException;
                }
                catch (MissingMethodException)
                {
                    throw new Exception("The arguments passed into the Activator do not match the constructor of the type being instantiated and may need to be updated.");
                }
            });
        }
    }
}
