using System;
using Xunit;

namespace REtry.Tests
{
    using System.Threading;
    using System.Threading.Tasks;

    public class REtryActionExtensionsTests
    {
        public REtryActionExtensionsTests()
        {
            Retry.RetryOperationFactory = (handlerInfo, totalInfo) => new FakeRetryOperation(handlerInfo, totalInfo);
        }

        [Fact]
        public async Task ThrowsOnceRetryHandlersAreExhausted()
        {
            var retryableOperation = Retry.Operation(() => throw new Exception())
                .WhenExceptionIs<Exception>(async (retry, ex) =>
                {
                    if (retry.Total.AttemptsExhausted < 3)
                    {
                        await retry.Wait(100);

                        return retry.Handled();
                    }

                    return retry.Unhandled();
                })
                .GetRetryableOperation();

            await Assert.ThrowsAsync<Exception>(async () => await retryableOperation(CancellationToken.None));
        }
    }
}
