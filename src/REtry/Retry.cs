using System;
using System.Threading;
using System.Threading.Tasks;

namespace REtry
{
    public static class Retry
    {
        public static RetryOperationBuilder<Func<CancellationToken, Task>> Operation(Func<CancellationToken, Task> operation)
        {
            return new RetryOperationBuilder<Func<CancellationToken, Task>>(operation);
        }

        public static RetryOperationBuilder<Func<Task>> Operation(Func<Task> operation)
        {
            return new RetryOperationBuilder<Func<Task>>(operation);
        }

        public static RetryOperationBuilder<Action> Operation(Action operation)
        {
            return new RetryOperationBuilder<Action>(operation);
        }

        public static Func<RetryHandlerInfo, RetryTotalInfo, IRetryOperation> RetryOperationFactory { internal get; set; } = 
            (handlerInfo, totalInfo) => new RetryOperation(handlerInfo, totalInfo);
    }
}
