using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Resiliency.Tests.Functions
{
    public class TimeoutTests
    {
        [Fact]
        public async Task TimeoutExceptionIsThrown()
        {
            var resilientOperation = ResilientOperation.From(async () =>
                {
                    await Task.Delay(100);

                    return 42;
                })
                .TimeoutAfter(TimeSpan.FromMilliseconds(20))
                .GetOperation();

            await Assert.ThrowsAsync<TimeoutException>(() => resilientOperation(CancellationToken.None));
        }

        [Fact]
        public async Task TimeoutExceptionRetried()
        {
            var retryWasHit = false;

            var resilientOperation = ResilientOperation.From(async () => 
                {
                    await Task.Delay(100);

                    return 42;
                })
                .TimeoutAfter(TimeSpan.FromMilliseconds(20))
                .WhenExceptionIs<TimeoutException>((op, ex) =>
                {
                    if (op.Total.AttemptsExhausted == 0)
                    {
                        retryWasHit = true;

                        return Task.FromResult(op.Handled());
                    }

                    return Task.FromResult(op.Unhandled());
                })
                .GetOperation();

            await Assert.ThrowsAsync<TimeoutException>(() => resilientOperation(CancellationToken.None));
            Assert.True(retryWasHit);
        }
    }
}