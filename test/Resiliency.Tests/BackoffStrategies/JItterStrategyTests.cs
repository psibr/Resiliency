using Resiliency.BackoffStrategies;
using Resiliency.BackoffStrategies.Jitter;
using System;
using Xunit;

namespace Resiliency.Tests.BackoffStrategies
{
    public class JitterStrategyTests
    {
        private class MinConstantRandomNumberGenerator : IRandomNumberGenerator
        {
            public int Next() => throw new NotImplementedException();
            public int Next(int maxValue) => throw new NotImplementedException();
            public int Next(int minValue, int maxValue) => throw new NotImplementedException();
            public void NextBytes(byte[] buffer) => throw new NotImplementedException();
            public double Next(double minValue, double maxValue)
                => NextDouble() * (maxValue - minValue) + minValue;

            public double NextDouble() => 0;
        }

        private class MaxConstantRandomNumberGenerator : IRandomNumberGenerator
        {
            public int Next() => throw new NotImplementedException();
            public int Next(int maxValue) => throw new NotImplementedException();
            public int Next(int minValue, int maxValue) => throw new NotImplementedException();

            public double Next(double minValue, double maxValue)
                => NextDouble() * (maxValue - minValue) + minValue;

            public void NextBytes(byte[] buffer) => throw new NotImplementedException();

            public double NextDouble() => .99999;
        }

        private readonly TimeSpan _waitTime = TimeSpan.FromSeconds(30);
        private readonly IBackoffStrategy _constantStrategy;
        private readonly MinConstantRandomNumberGenerator _minRandomNumberGenerator;
        private readonly MaxConstantRandomNumberGenerator _maxRandomNumberGenerator;

        private readonly double rangeOfError;

        public JitterStrategyTests()
        {
            _constantStrategy = new ConstantBackoffStrategy(_waitTime);

            _minRandomNumberGenerator = new MinConstantRandomNumberGenerator();
            _maxRandomNumberGenerator = new MaxConstantRandomNumberGenerator();

            rangeOfError = 1 - (1 - _maxRandomNumberGenerator.NextDouble()) * 10; // Let's be generous with our buffer.
        }

        [Fact]
        public void FullJitterMinimuimWaitTimeIsZero()
        {
            var strategyWithJitter = _constantStrategy.WithFullJitter();

            ((IRequireRandom)strategyWithJitter).RandomNumberGenerator = _minRandomNumberGenerator;

            var waitTime = strategyWithJitter.Next();

            Assert.Equal(TimeSpan.Zero, waitTime);
        }

        [Fact]
        public void FullJitterMaximumWaitTimeIsAlmostTheFullWait()
        {
            var strategyWithJitter = _constantStrategy.WithFullJitter();

            ((IRequireRandom)strategyWithJitter).RandomNumberGenerator = _maxRandomNumberGenerator;

            var waitTime = strategyWithJitter.Next();

            Assert.True(_waitTime * rangeOfError < waitTime);
        }

        [Fact]
        public void EqualJitterMinimuimWaitTimeIsHalfTheWaitTime()
        {
            var strategyWithJitter = _constantStrategy.WithEqualJitter();

            ((IRequireRandom)strategyWithJitter).RandomNumberGenerator = _minRandomNumberGenerator;

            var waitTime = strategyWithJitter.Next();

            Assert.Equal(TimeSpan.FromMilliseconds(_waitTime.TotalMilliseconds / 2), waitTime);
        }

        [Fact]
        public void EqualJitterMaximumWaitTimeIsAlmostTheFullWait()
        { 
            var strategyWithJitter = _constantStrategy.WithEqualJitter();

            ((IRequireRandom)strategyWithJitter).RandomNumberGenerator = _maxRandomNumberGenerator;

            var waitTime = strategyWithJitter.Next();

            Assert.True(_waitTime * rangeOfError < waitTime);
        }

        [Fact]
        public void DecorrelatedJitterMinimuimWaitTimeIsTheWaitTime()
        {
            var strategyWithJitter = _constantStrategy.WithDecorrelatedJitter();

            ((IRequireRandom)strategyWithJitter).RandomNumberGenerator = _minRandomNumberGenerator;

            var waitTime = strategyWithJitter.Next();

            Assert.Equal(_waitTime, waitTime);
        }

        [Fact]
        public void DecorrelatedJitterMaximumWaitTimeIsAlmostThreeTimesTheLastWait()
        {
            var strategyWithJitter = _constantStrategy.WithDecorrelatedJitter();

            ((IRequireRandom)strategyWithJitter).RandomNumberGenerator = _maxRandomNumberGenerator;

            var waitTime = strategyWithJitter.Next();

            Assert.True(_waitTime * 3 * rangeOfError < waitTime);
        }

        [Fact]
        public void NullStrategyThrowsException()
        {
            IBackoffStrategy decoratedStrategy = null;
            IRandomNumberGenerator randomNumberGenerator = new DefaultRandomNumberGenerator();

            Assert.Throws<ArgumentNullException>(() =>
            {
                new MultiplicativeJitterBackoffStrategy(decoratedStrategy, 0, 1);
            });
        }
    }
}