using System;
using Xunit;

namespace Resiliency.Tests.Functions
{
    using System.Threading;
    using System.Threading.Tasks;

    public class ResilentFunctionsReturnTests
    {
        public ResilentFunctionsReturnTests()
        {
            ResilientOperation.WaiterFactory = (cancellationToken) => new FakeWaiter(cancellationToken);
        }

        [Fact]
        public async Task ResilientFunctionReturnsAfterRetries()
        {
            int failureCount = 0;

            var resilientOperation = ResilientOperation.From(() =>
                {
                    // Fail 3 times before succeeding
                    if (failureCount < 3)
                    {
                        failureCount++;
                        throw new Exception();
                    }

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

            Assert.Equal(42, await resilientOperation(CancellationToken.None));
        }
    }
}
