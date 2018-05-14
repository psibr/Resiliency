using System;
using System.Threading.Tasks;

namespace Resiliency
{
    public static class ResilientOperationExtensions
    {
        public static HandlerResult Handled(this ResilientOperation resilientOperation)
        {
            if (resilientOperation.CancellationToken.IsCancellationRequested)
                return HandlerResult.Cancelled;

            return HandlerResult.Handled;
        }

        public static HandlerResult Unhandled(this ResilientOperation retry) => HandlerResult.Unhandled;

        public static Task WaitAsync(this ResilientOperation retry, TimeSpan period) => 
            ResilientOperation.WaiterFactory(retry.CancellationToken).WaitAsync(period);
    }
}
