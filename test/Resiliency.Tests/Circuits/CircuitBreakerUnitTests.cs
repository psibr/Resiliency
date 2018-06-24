using System;
using Xunit;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency.Tests.Circuits
{
    public class CircuitBreakerUnitTests
    {
        [Fact]
        public void RetrievesCircuitBreakerFromKey()
        {
            var strategy = new ConsecutiveFailureCircuitBreakerStrategy(3);

            var circuitBreakerInstance = new CircuitBreaker(strategy);

            CircuitBreaker.Panel.Add("ConsecutiveFailureCircuitBreakerStrategy", circuitBreakerInstance);

            Assert.Equal(circuitBreakerInstance, CircuitBreaker.Panel["ConsecutiveFailureCircuitBreakerStrategy"]);
        }

        [Fact]
        public void ThrowsErrorWhenNoRegisteredCircuitBreaker()
        {
            Assert.Throws<ArgumentException>(() => CircuitBreaker.Panel["ThrowsErrorWhenNoRegisteredCircuitBreaker"]);
        }

        [Fact]
        public void CreatesCircuitBreakerWhenNone()
        {
            CircuitBreaker CircuitBreakerFactory() => new CircuitBreaker(new ConsecutiveFailureCircuitBreakerStrategy(9));

            var circuitBreaker = CircuitBreaker.Panel.GetOrAdd("CreatesCircuitBreakerWhenNone", CircuitBreakerFactory);

            Assert.NotNull(circuitBreaker);
        }

        [Fact]
        public void RegisterCircuitBreakerWhenExists()
        {
            CircuitBreaker CircuitBreakerFactory() => new CircuitBreaker(new ConsecutiveFailureCircuitBreakerStrategy(9));

            CircuitBreaker.Panel.GetOrAdd("RegisterCircuitBreakerWhenExists", CircuitBreakerFactory);

            Assert.Throws<InvalidOperationException>(() => CircuitBreaker.Panel.Add("RegisterCircuitBreakerWhenExists", new CircuitBreaker(new ConsecutiveFailureCircuitBreakerStrategy(2))));
        }
    }
}