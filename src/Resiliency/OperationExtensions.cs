using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    public static class OperationExtensions
    {
        /// <summary>
        /// Create a <see cref="ResilientOperationBuilder&lt;TOperation&gt;"/> from an existing async action with cancellation support. 
        /// </summary>
        /// <param name="operation">An async action, that supports cancellation, that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientOperationBuilder<Func<CancellationToken, Task>> AsResilient(this Func<CancellationToken, Task> operation) =>
            Resiliency.ResilientOperation.From(operation);

        /// <summary>
        /// Create a <see cref="ResilientOperationBuilder&lt;TOperation&gt;"/> from an existing async action. 
        /// </summary>
        /// <param name="operation">An async action that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientOperationBuilder<Func<Task>> AsResilient(this Func<Task> operation) =>
            Resiliency.ResilientOperation.From(operation);

        /// <summary>
        /// Create a <see cref="ResilientOperationBuilder&lt;TOperation&gt;"/> from an existing synchronous action. 
        /// </summary>
        /// <param name="operation">A synchronous action that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientOperationBuilder<Action> AsResilient(this Action operation) =>
            Resiliency.ResilientOperation.From(operation);
    }
}
