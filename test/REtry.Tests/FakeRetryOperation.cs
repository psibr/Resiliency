using System;

namespace REtry.Tests
{
    using System.Threading;
    using System.Threading.Tasks;

    public class FakeRetryOperation
        : IRetryOperation
    {
        public FakeRetryOperation(RetryHandlerInfo handler, RetryTotalInfo total)
        {
            Handler = handler;
            Total = total;
        }

        public RetryHandlerInfo Handler { get; }

        public RetryTotalInfo Total { get; }

        public Task Wait(int milliseconds, CancellationToken cancellationToken = default(CancellationToken)) => 
            Wait(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);

        public Task Wait(TimeSpan period, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }
    }
}
