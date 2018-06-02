using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Resiliency.Tests.Functions
{
    public class BackoffStrategyTests
    {
        public BackoffStrategyTests()
        {
            ResilientOperation.WaiterFactory = (cancellationToken) => new FakeWaiter(cancellationToken);
        }

        [Fact]
        public async Task ExceptionHandlersIncrementBackoff()
        {
            var backoffStrategy = Backoff
                .LinearlyFrom(TimeSpan.FromMilliseconds(250));

            int failureCount = 0;

            var resilientOperation = ResilientOperation.From(() =>
            {
                // Fail 3 times before succeeding
                if (failureCount < 3)
                {
                    failureCount++;
                    throw new Exception();
                }

                if (failureCount < 4)
                {
                    failureCount++;
                    return Task.FromResult(5);
                }

                return Task.FromResult(42);
            })
            .WhenExceptionIs<Exception>(
                backoffStrategy,
                async (op, ex) =>
                {
                    if (op.CurrentAttempt <= 3)
                    {
                        await op.RetryAfterAsync(op.BackoffStrategy.Next());
                    }
                })
            .WhenResult(value => value != 42,
                backoffStrategy,
                async (op, value) =>
                {
                    if (op.CurrentAttempt <= 4)
                    {
                        await op.RetryAfterAsync(op.BackoffStrategy.Next());
                    }
                })
            .GetOperation();


            Assert.Equal(42, await resilientOperation(CancellationToken.None));
            Assert.Equal(4, failureCount);
            Assert.Equal(TimeSpan.FromMilliseconds(250 * 5), backoffStrategy.Next());

        }
    }
}
