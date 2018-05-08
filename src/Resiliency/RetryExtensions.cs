using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    public static class RetryExtensions
    {
        public static ResilientOperationBuilder<Func<CancellationToken, Task>> AsResilient(this Func<CancellationToken, Task> operation) =>
            Resiliency.ResilientOperation.From(operation);

        public static ResilientOperationBuilder<Func<Task>> AsResilient(this Func<Task> operation) =>
            Resiliency.ResilientOperation.From(operation);

        public static ResilientOperationBuilder<Action> AsResilient(this Action operation) =>
            Resiliency.ResilientOperation.From(operation);
    }
}
