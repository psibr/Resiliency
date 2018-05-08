using System;
using System.Threading;
using System.Threading.Tasks;

namespace REtry
{
    public static class RetryExtensions
    {
        public static ResilientOperationBuilder<Func<CancellationToken, Task>> Retry(this Func<CancellationToken, Task> operation) =>
            REtry.Operation.From(operation);

        public static ResilientOperationBuilder<Func<Task>> Retry(this Func<Task> operation) =>
            REtry.Operation.From(operation);

        public static ResilientOperationBuilder<Action> Retry(this Action operation) =>
            REtry.Operation.From(operation);
    }
}
