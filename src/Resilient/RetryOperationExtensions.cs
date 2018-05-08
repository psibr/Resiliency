using System;
using System.Threading.Tasks;

namespace REtry
{
    public static class RetryOperationExtensions
    {
        public static RetryHandlerResult Handled(this RetryOperation retry)
        {
            if (retry.CancellationToken.IsCancellationRequested)
                return RetryHandlerResult.Cancelled;

            return RetryHandlerResult.Handled;
        }

        public static RetryHandlerResult Unhandled(this RetryOperation retry) => RetryHandlerResult.Unhandled;

        public static Task WaitAsync(this RetryOperation retry, TimeSpan period) => 
            Operation.WaiterFactory(retry.CancellationToken).WaitAsync(period);
    }

    public enum RetryHandlerResult
    {
        Unhandled = 0,
        Handled = 1,
        Cancelled = 2
    }
}
