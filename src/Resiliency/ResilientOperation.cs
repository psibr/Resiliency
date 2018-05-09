using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    public static class ResilientOperation
    {

        /// <summary>
        /// Create a <see cref="ResilientOperationBuilder&lt;TOperation&gt;"/> from an existing async action with cancellation support. 
        /// </summary>
        /// <param name="operation">An async action, that supports cancellation, that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientOperationBuilder<Func<CancellationToken, Task>> From(Func<CancellationToken, Task> operation)
        {
            return new ResilientOperationBuilder<Func<CancellationToken, Task>>(operation);
        }

        /// <summary>
        /// Create a <see cref="ResilientOperationBuilder&lt;TOperation&gt;"/> from an existing async action. 
        /// </summary>
        /// <param name="operation">An async action that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientOperationBuilder<Func<Task>> From(Func<Task> operation)
        {
            return new ResilientOperationBuilder<Func<Task>>(operation);
        }

        /// <summary>
        /// Create a <see cref="ResilientOperationBuilder&lt;TOperation&gt;"/> from an existing synchronous action. 
        /// </summary>
        /// <param name="operation">A synchronous action that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientOperationBuilder<Action> From(Action operation)
        {
            return new ResilientOperationBuilder<Action>(operation);
        }

        /// <summary>
        /// Provide an implementation of IWaiter that can use mechanisms other than Task.Delay, such as for Unit tests with mocks.
        /// </summary>
        /// <returns>A function that given a <see cref="CancellationToken"/>, will get a waiter object.</returns>
        public static Func<CancellationToken, IWaiter> WaiterFactory { internal get; set; } =
            (cancellationToken) => new TaskDelayWaiter(cancellationToken);
    }
}
