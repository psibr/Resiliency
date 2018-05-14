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
                .WhenExceptionIs<Exception>(async (op, ex) =>
                {
                    if (op.Total.AttemptsExhausted < 3)
                    {
                        await op.WaitAsync(TimeSpan.FromMilliseconds(100));

                        return op.Handled();
                    }

                    return op.Unhandled();
                })
                .GetOperation();


            Assert.Equal(42, await resilientOperation(CancellationToken.None));
        }
    }
}
