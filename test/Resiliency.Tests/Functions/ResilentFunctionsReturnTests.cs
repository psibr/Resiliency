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
                    
                    if (failureCount < 4)
                    {
                        failureCount++;
                        return 5;
                    }

                    return 42;
                })
                .WhenExceptionIs<Exception>(async (op, ex) =>
                {
                    if (op.Total.AttemptsExhausted < 3)
                    {
                        await op.WaitThenRetryAsync(TimeSpan.FromMilliseconds(100));
                    }
                })
                .WhenResult(i => i < 42, async (op, i) =>
                {
                    await op.WaitThenRetryAsync(TimeSpan.FromMilliseconds(100));
                })
                .GetOperation();


            Assert.Equal(42, await resilientOperation(CancellationToken.None));
        }
    }
}
