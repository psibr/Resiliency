using System;
using System.Threading.Tasks;

namespace Resiliency
{
    public static class ResilientOperationExtensions
    {
        public static Task WaitAsync(this IResilientOperation op, TimeSpan period) => 
            ResilientOperation.WaiterFactory(op.CancellationToken).WaitAsync(period);

        /// <summary>
        /// Causes a wait operation for the provided time and subsequently informs the operation to retry once the handler finishes execution.
        /// </summary>
        /// <param name="op">The operation being handled</param>
        /// <param name="period">A period to wait</param>
        /// <returns></returns>
        public static async Task RetryAfterAsync(this IResilientOperation op, TimeSpan period)
        {
            await op.WaitAsync(period).ConfigureAwait(false);

            op.Retry();
        }
    }
}
