using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Resiliency.Tests.Actions
{
    public class CircuitBreakerTests
    {

        public CircuitBreakerTests()
        {
            ResilientOperation.WaiterFactory = (cancellationToken) => new FakeWaiter(cancellationToken);
        }

        [Fact]
        public async Task ExplicitTripWorks()
        {
            var resilientOperation = ResilientOperation
                .From(() => throw new Exception())
                .WhenExceptionIs<Exception>(async (op, ex) =>
                {
                    if (op.CurrentAttempt <= 3)
                    {
                        await op.WaitThenRetryAsync(TimeSpan.FromMilliseconds(100));
                    }
                    else
                    {
                        op.DefaultCircuitBreaker.Trip(ex, TimeSpan.FromDays(1));
                    }
                })
                .GetOperation();

            await Assert.ThrowsAsync<CircuitBrokenException>(async () => await resilientOperation(CancellationToken.None));
        }

        [Fact]
        public async Task CircuitBreaksAfterConsecutiveFailures()
        {
            var resilientOperation = ResilientOperation
                .From(() => throw new Exception())
                .WithCircuitBreaker(
                    operationKey: nameof(CircuitBreaksAfterConsecutiveFailures), 
                    onMissingFactory: () => new CircuitBreaker(new ConsecutiveFailureCircuitBreakerStrategy(3)))
                .WhenExceptionIs<Exception>(async (op, ex) =>
                {
                    if (op.CurrentAttempt <= 3)
                    {
                        await op.WaitThenRetryAsync(TimeSpan.FromMilliseconds(100));
                    }
                })
                .GetOperation();

            await Assert.ThrowsAsync<CircuitBrokenException>(async () => await resilientOperation(CancellationToken.None));
        }
    }
}
