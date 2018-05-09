using System;
using Xunit;

namespace Resiliency.Tests.Functions
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
            var resilientOperation = ResilientOperation.From(() =>
                {
                    throw new Exception();

                    return 42;
                })
                .RetryWhenExceptionIs<Exception>(async (retry, ex) =>
                {
                    if (retry.Total.AttemptsExhausted < 3)
                    {
                        await retry.WaitAsync(TimeSpan.FromMilliseconds(100));

                        return retry.Handled();
                    }

                    return retry.Unhandled();
                })
                .GetOperation();

            await Assert.ThrowsAsync<Exception>(async () => await resilientOperation(CancellationToken.None));
        }

        [Fact]
        public async Task ExtensionThrowsOnceRetryHandlersAreExhausted()
        {
            Func<Task<int>> asyncOperation = () =>
            {
                throw new Exception();

                return Task.FromResult(42);
            };

            var resilientOperation = asyncOperation
                .AsResilient()
                .RetryWhenExceptionIs<Exception>(async (retry, ex) =>
                {
                    if (retry.Total.AttemptsExhausted < 3)
                    {
                        await retry.WaitAsync(TimeSpan.FromMilliseconds(100));

                        return retry.Handled();
                    }

                    return retry.Unhandled();
                })
                .GetOperation();

                await Assert.ThrowsAsync<Exception>(async () => await resilientOperation(CancellationToken.None));
        }
    }
}
