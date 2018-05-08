using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    public static class ResilientOperation
    {
        public static ResilientOperationBuilder<Func<CancellationToken, Task>> From(Func<CancellationToken, Task> operation)
        {
            return new ResilientOperationBuilder<Func<CancellationToken, Task>>(operation);
        }

        public static ResilientOperationBuilder<Func<Task>> From(Func<Task> operation)
        {
            return new ResilientOperationBuilder<Func<Task>>(operation);
        }

        public static ResilientOperationBuilder<Action> From(Action operation)
        {
            return new ResilientOperationBuilder<Action>(operation);
        }

        public static Func<CancellationToken, IWaiter> WaiterFactory { internal get; set; } =
            (cancellationToken) => new TaskDelayWaiter(cancellationToken);
    }
}
