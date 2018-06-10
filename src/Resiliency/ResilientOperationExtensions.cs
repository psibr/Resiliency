using System;
using System.Threading.Tasks;

namespace Resiliency
{
    public static class ResilientOperationExtensions
    {
        public static Task WaitAsync(this IResilientOperation op, TimeSpan period) => 
            ResilientOperation.WaiterFactory(op.CancellationToken).WaitAsync(period);

        public static async Task RetryAfterAsync(this IResilientOperation op, TimeSpan period)
        {
            await op.WaitAsync(period).ConfigureAwait(false);

            op.Retry();
        }
    }
}
