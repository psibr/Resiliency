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
                    if (op.CurrentAttempt == 1)
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