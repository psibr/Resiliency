using System;
using System.Threading.Tasks;

namespace Resiliency
{
    public static class ResilientOperationExtensions
    {
        public static Task WaitAsync(this ResilientOperation op, TimeSpan period) => 
            ResilientOperation.WaiterFactory(op.CancellationToken).WaitAsync(period);

        public static async Task RetryAfterAsync(this ResilientOperation op, TimeSpan period)
        {
            await op.WaitAsync(period).ConfigureAwait(false);

            op.Retry();
        }
    }
}
