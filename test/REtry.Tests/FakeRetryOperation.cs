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

        public CancellationToken CancellationToken { get; }

        public Task Wait(int milliseconds) => 
            Wait(TimeSpan.FromMilliseconds(milliseconds));

        public Task Wait(TimeSpan period)
        {
            if (CancellationToken.IsCancellationRequested)
                return Task.FromCanceled(CancellationToken);

            return Task.CompletedTask;
        }
    }
}
