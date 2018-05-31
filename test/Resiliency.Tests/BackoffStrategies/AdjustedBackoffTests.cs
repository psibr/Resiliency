using Resiliency.BackoffStrategies;
using System;
using Xunit;

namespace Resiliency.Tests.BackoffStrategies
{
    public class AdjustedBackoffTests
    {
        private class NegativeResultBackoffStrategy : IBackoffStrategy
        {
            public TimeSpan InitialWaitTime => TimeSpan.FromSeconds(12);

            public TimeSpan Next()
            {
                return InitialWaitTime * -1; // Sneaky.
            }
        }

        private readonly IBackoffStrategy _constantStrategy;

        public AdjustedBackoffTests()
        {
            _constantStrategy = new ConstantBackoffStrategy(TimeSpan.FromSeconds(12));
        }

        #region Constant Adjustment Tests

        [Fact]
        public void ThrowsExceptionWhenConstantAdjustmentIsGreaterThanInitialWait()
        {
            Assert.Throws<ArgumentException>(() => _constantStrategy.WithAdjustment(-_constantStrategy.InitialWaitTime * 2));
        }

        [Fact]
        public void AcceptsConstantAdjustmentEqualToInitialWait()
        {
            var adjustedStrategy = _constantStrategy.WithAdjustment(-_constantStrategy.InitialWaitTime);
            var waitTime = adjustedStrategy.Next();

            Assert.Equal(TimeSpan.Zero, waitTime);
        }

        [Fact]
        public void ConstantAdjustmentDefaultsNegativeResultToZero()
        {
            var adjustedStrategy = new NegativeResultBackoffStrategy().WithAdjustment(TimeSpan.FromSeconds(0));
            var waitTime = adjustedStrategy.Next();

            Assert.Equal(TimeSpan.Zero, waitTime);
        }

        [Fact]
        public void NegativeConstantAdjustmentReducesWaitTime()
        {
            TimeSpan adjustment = -(_constantStrategy.InitialWaitTime / 2); 
            var adjustedStrategy =_constantStrategy.WithAdjustment(adjustment);
            var waitTime = adjustedStrategy.Next();

            Assert.Equal(_constantStrategy.InitialWaitTime + adjustment, waitTime);
        }

        [Fact]
        public void PositiveConstantAdjustmentIncreasesWaitTime()
        {
            TimeSpan adjustment = _constantStrategy.InitialWaitTime / 2;
            var adjustedStrategy = _constantStrategy.WithAdjustment(adjustment);
            var waitTime = adjustedStrategy.Next();

            Assert.Equal(_constantStrategy.InitialWaitTime + adjustment, waitTime);
        }

        [Fact]
        public void NullStrategyThrowsExceptionForAConstantAdjustment()
        {
            IBackoffStrategy decoratedStrategy = null;
            IRandomNumberGenerator randomNumberGenerator = new DefaultRandomNumberGenerator();

            Assert.Throws<ArgumentNullException>(() =>
            {
                new ConstantAdjustedBackoffStrategy(decoratedStrategy, TimeSpan.Zero);
            });
        }

        #endregion

        #region Percentage Adjustment Tests

        [Fact]
        public void ThrowsExceptionWhenPercentageAdjustmentIsNegative()
        {
            Assert.Throws<ArgumentException>(() => _constantStrategy.WithAdjustment(-.5));
        }

        [Fact]
        public void AcceptsZeroPercentAdjustment()
        {
            var adjustedStrategy = _constantStrategy.WithAdjustment(-_constantStrategy.InitialWaitTime);
            var waitTime = adjustedStrategy.Next();

            Assert.Equal(TimeSpan.Zero, waitTime);
        }

        [Fact]
        public void PercentageLowerThan1DecreasesWaitTime()
        {
            var adjustedStrategy = _constantStrategy.WithAdjustment(.5);
            var waitTime = adjustedStrategy.Next();

            Assert.Equal(_constantStrategy.InitialWaitTime / 2, waitTime);
        }

        [Fact]
        public void PercentageGreaterThan1IncreasesWaitTime()
        {
            var adjustedStrategy = _constantStrategy.WithAdjustment(1.5);
            var waitTime = adjustedStrategy.Next();

            Assert.Equal(_constantStrategy.InitialWaitTime * 1.5, waitTime);
        }

        [Fact]
        public void PercentageEqualTo1DoesNotChangeWaitTime()
        {
            var adjustedStrategy = _constantStrategy.WithAdjustment(1);
            var waitTime = adjustedStrategy.Next();

            Assert.Equal(_constantStrategy.InitialWaitTime, waitTime);
        }

        [Fact]
        public void NullStrategyThrowsExceptionForAPercentageAdjustment()
        {
            IBackoffStrategy decoratedStrategy = null;
            IRandomNumberGenerator randomNumberGenerator = new DefaultRandomNumberGenerator();

            Assert.Throws<ArgumentNullException>(() =>
            {
                new PercentageAdjustedBackoffStrategy(decoratedStrategy, 1);
            });
        }

        #endregion
    }
}
