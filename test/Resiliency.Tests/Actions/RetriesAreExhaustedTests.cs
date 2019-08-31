using System;
using Xunit;

namespace Resiliency.Tests.Actions
{
    using System.Threading;
    using System.Threading.Tasks;

    public class RetriesAreExhaustedTests
    {
        public RetriesAreExhaustedTests()
        {
            ResilientOperation.WaiterFactory = (cancellationToken) => new FakeWaiter(cancellationToken);
        }

        [Fact]
        public Task ThrowsOnceRetryHandlersAreExhausted()
        {
            var resilientOperation = ResilientOperation.From(() => throw new Exception())
                .WhenExceptionIs<Exception>(async (op, ex) =>
                {
                    if (op.CurrentAttempt <= 3)
                    {
                        await op.RetryAfterAsync(TimeSpan.FromMilliseconds(100));
                    }
                })
                .GetOperation();

            return Assert.ThrowsAsync<Exception>(async () => await resilientOperation(CancellationToken.None));
        }

        [Fact]
        public Task ExtensionThrowsOnceRetryHandlersAreExhausted()
        {
            Func<Task> asyncOperation = () => throw new Exception();

            var resilientOperation = asyncOperation
                .AsResilient()
                .WhenExceptionIs<Exception>(async (op, ex) =>
                {
                    if (op.CurrentAttempt <= 3)
                    {
                        await op.RetryAfterAsync(TimeSpan.FromMilliseconds(100));
                    }
                })
                .GetOperation();

            return Assert.ThrowsAsync<Exception>(async () => await resilientOperation(CancellationToken.None));
        }
    }
}
