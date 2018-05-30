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
        public async Task ThrowsOnceRetryHandlersAreExhausted()
        {
            var resilientOperation = ResilientOperation.From(() => throw new Exception())
                .WhenExceptionIs<Exception>(async (op, ex) =>
                {
                    if (op.CurrentAttempt <= 3)
                    {
                        await op.WaitThenRetryAsync(TimeSpan.FromMilliseconds(100));
                    }
                })
                .GetOperation();

            await Assert.ThrowsAsync<Exception>(async () => await resilientOperation(CancellationToken.None));
        }

        [Fact]
        public async Task ExtensionThrowsOnceRetryHandlersAreExhausted()
        {
            Func<Task> asyncOperation = () => throw new Exception();

            var resilientOperation = asyncOperation
                .AsResilient()
                .WhenExceptionIs<Exception>(async (op, ex) =>
                {
                    if (op.CurrentAttempt <= 3)
                    {
                        await op.WaitThenRetryAsync(TimeSpan.FromMilliseconds(100));
                    }
                })
                .GetOperation();

                await Assert.ThrowsAsync<Exception>(async () => await resilientOperation(CancellationToken.None));
        }
    }
}
