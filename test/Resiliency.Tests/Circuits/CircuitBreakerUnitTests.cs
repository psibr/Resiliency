using System;
using Xunit;

namespace Resiliency.Tests.Circuits
{
    public class CircuitBreakerUnitTests
    {
        [Fact]
        public void RetrievesCircuitBreakerFromKey()
        {
            var strategy = new ConsecutiveFailureCircuitBreakerStrategy(3);

            var circuitBreakerInstance = new CircuitBreaker(strategy);

            CircuitBreaker.RegisterCircuitBreaker("ConsecutiveFailureCircuitBreakerStrategy", circuitBreakerInstance);

            Assert.Equal<CircuitBreaker>(circuitBreakerInstance, CircuitBreaker.GetCircuitBreaker("ConsecutiveFailureCircuitBreakerStrategy"));
        }

        [Fact]
        public void ThrowsErrorWhenNoRegisteredCircuitBreaker()
        {
            Assert.Throws<ArgumentException>(() => CircuitBreaker.GetCircuitBreaker("ThrowsErrorWhenNoRegisteredCircuitBreaker"));
        }

        [Fact]
        public void CreatesCircuitBreakerWhenNone()
        {
            Func<CircuitBreaker> circuitBreakerFactory = () => new CircuitBreaker(new ConsecutiveFailureCircuitBreakerStrategy(9));

            var circuitBreaker = CircuitBreaker.GetOrAddCircuitBreaker("CreatesCircuitBreakerWhenNone", circuitBreakerFactory);

            Assert.NotNull(circuitBreaker);
        }

        [Fact]
        public void RegisterCircuitBreakerWhenExists()
        {
            Func<CircuitBreaker> circuitBreakerFactory = () => new CircuitBreaker(new ConsecutiveFailureCircuitBreakerStrategy(9));

            var circuitBreaker = CircuitBreaker.GetOrAddCircuitBreaker("RegisterCircuitBreakerWhenExists", circuitBreakerFactory);

            Assert.Throws<InvalidOperationException>(() => CircuitBreaker.RegisterCircuitBreaker("RegisterCircuitBreakerWhenExists", new CircuitBreaker(new ConsecutiveFailureCircuitBreakerStrategy(2))));
        }
    }
}