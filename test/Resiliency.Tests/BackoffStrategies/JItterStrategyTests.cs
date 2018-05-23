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

            public double NextDouble() => 0;
        }

        private class MaxConstantRandomNumberGenerator : IRandomNumberGenerator
        {
            public int Next() => throw new NotImplementedException();
            public int Next(int maxValue) => throw new NotImplementedException();
            public int Next(int minValue, int maxValue) => throw new NotImplementedException();
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
        public void FullJitterMinimuimWaitTimeIsTheWaitTime()
        {
            var strategyWithJitter = _constantStrategy.WithFullJitter(_minRandomNumberGenerator);
            var attempts = 1;

            var waitTime = strategyWithJitter.GetWaitTime(attempts);

            Assert.Equal(_waitTime, waitTime);
        }

        [Fact]
        public void FullJitterMaximumWaitTimeIsAlmostTwiceTheFullWait()
        {
            var strategyWithJitter = _constantStrategy.WithFullJitter(_maxRandomNumberGenerator);
            var attempts = 1;

            var waitTime = strategyWithJitter.GetWaitTime(attempts);

            Assert.True(_waitTime * 2 * rangeOfError < waitTime);
        }

        [Fact]
        public void HalfJitterMinimuimWaitTimeIsTheWaitTime()
        {
            var strategyWithJitter = _constantStrategy.WithEqualJitter(_minRandomNumberGenerator);
            var attempts = 1;

            var waitTime = strategyWithJitter.GetWaitTime(attempts);

            Assert.Equal(_waitTime, waitTime);
        }

        [Fact]
        public void HalfJitterMaximumWaitTimeIsAlmostTheFullWaitPlusHalf()
        {
            var strategyWithJitter = _constantStrategy.WithEqualJitter(_maxRandomNumberGenerator);
            var attempts = 1;

            var waitTime = strategyWithJitter.GetWaitTime(attempts);

            Assert.True(_waitTime * 1.5 * rangeOfError < waitTime);
        }

        [Fact]
        public void DecorrelatedJitterMinimuimWaitTimeIsTheWaitTime()
        {
            var strategyWithJitter = _constantStrategy.WithDecorrelatedJitter(_waitTime, _minRandomNumberGenerator);
            var attempts = 1;

            var waitTime = strategyWithJitter.GetWaitTime(attempts);

            Assert.Equal(_waitTime, waitTime);
        }

        [Fact]
        public void DecorrelatedJitterMaximumWaitTimeIsAlmostThreeTimesTheLastWait()
        {
            var strategyWithJitter = _constantStrategy.WithDecorrelatedJitter(_waitTime, _maxRandomNumberGenerator);
            var attempts = 1;

            var waitTime = strategyWithJitter.GetWaitTime(attempts);

            Assert.True(_waitTime * 3 * rangeOfError < waitTime);
        }
    }
}