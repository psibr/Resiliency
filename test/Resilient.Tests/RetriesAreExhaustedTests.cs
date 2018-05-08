using System;
using Xunit;

namespace REtry.Tests
{
    using System.Threading;
    using System.Threading.Tasks;

    public class RetriesAreExhaustedTests
    {
        public RetriesAreExhaustedTests()
        {
            Operation.WaiterFactory = (cancellationToken) => new FakeWaiter(cancellationToken);
        }

        [Fact]
        public async Task ThrowsOnceRetryHandlersAreExhausted()
        {
            var resilientOperation = Operation.From(() => throw new Exception())
                .WhenExceptionIs<Exception>(async (retry, ex) =>
                {
                    if (retry.Total.AttemptsExhausted < 3)
                    {
                        await retry.WaitAsync(TimeSpan.FromMilliseconds(100));

                        return retry.Handled();
                    }

                    return retry.Unhandled();
                })
                .GetResilientOperation();

            await Assert.ThrowsAsync<Exception>(async () => await resilientOperation(CancellationToken.None));
        }

        [Fact]
        public async Task ExtensionThrowsOnceRetryHandlersAreExhausted()
        {
            Func<Task> asyncOperation = () => throw new Exception();

            var resilientOperation = asyncOperation
                .Retry()
                .WhenExceptionIs<Exception>(async (retry, ex) =>
                {
                    if (retry.Total.AttemptsExhausted < 3)
                    {
                        await retry.WaitAsync(TimeSpan.FromMilliseconds(100));

                        return retry.Handled();
                    }

                    return retry.Unhandled();
                })
                .GetResilientOperation();

                await Assert.ThrowsAsync<Exception>(async () => await resilientOperation(CancellationToken.None));
        }
    }
}
