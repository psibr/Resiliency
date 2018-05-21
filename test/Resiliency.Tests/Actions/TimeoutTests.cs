using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Resiliency.Tests.Actions
{
    public class TimeoutTests
    {
        [Fact]
        public async Task TimeoutExceptionIsThrown()
        {
            var resilientOperation = ResilientOperation.From(() => Task.Delay(100))
                .TimeoutAfter(TimeSpan.FromMilliseconds(20))
                .GetOperation();

            await Assert.ThrowsAsync<TimeoutException>(() => resilientOperation(CancellationToken.None));
        }

        [Fact]
        public async Task TimeoutExceptionRetried()
        {
            var retryWasHit = false;

            var resilientOperation = ResilientOperation.From(() => Task.Delay(100))
                .TimeoutAfter(TimeSpan.FromMilliseconds(20))
                .WhenExceptionIs<TimeoutException>((op, ex) =>
                {
                    if (op.Total.AttemptsExhausted == 0)
                    {
                        retryWasHit = true;

                        op.Retry();
                    }

                    return Task.CompletedTask;
                })
                .GetOperation();

            await Assert.ThrowsAsync<TimeoutException>(() => resilientOperation(CancellationToken.None));
            Assert.True(retryWasHit);
        }
    }
}