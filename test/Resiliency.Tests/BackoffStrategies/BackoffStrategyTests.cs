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

        [Theory]
        [InlineData(typeof(MultiplicativeJitterBackoffStrategy))]
        public void NullStrategyInDecoratorThrowsException(Type decoratorType)
        {
            IBackoffStrategy decoratedStrategy = null;
            IRandomNumberGenerator randomNumberGenerator = new DefaultRandomNumberGenerator();

            Assert.Throws<ArgumentNullException>(() =>
            {
                try
                {
                    Activator.CreateInstance(decoratorType, new object[] { decoratedStrategy, 0d, 1d, randomNumberGenerator });
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
