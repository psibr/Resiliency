using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    public static class ResilientOperation
    {
        /// <summary>
        /// Create a <see cref="ResilientActionBuilder{TAction}"/> from an existing async action with cancellation support. 
        /// </summary>
        /// <param name="action">An async action, that supports cancellation, that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientActionBuilder<Func<CancellationToken, Task>> From(Func<CancellationToken, Task> action)
        {
            return new ResilientActionBuilder<Func<CancellationToken, Task>>(action);
        }

        /// <summary>
        /// Create a <see cref="ResilientActionBuilder{TAction}"/> from an existing async action. 
        /// </summary>
        /// <param name="action">An async action that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientActionBuilder<Func<Task>> From(Func<Task> action)
        {
            return new ResilientActionBuilder<Func<Task>>(action);
        }

        /// <summary>
        /// Create a <see cref="ResilientActionBuilder{TAction}"/> from an existing synchronous action. 
        /// </summary>
        /// <param name="action">A synchronous action that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientActionBuilder<Action> From(Action action)
        {
            return new ResilientActionBuilder<Action>(action);
        }

        /// <summary>
        /// Provide an implementation of IWaiter that can use mechanisms other than Task.Delay, such as for Unit tests with mocks.
        /// </summary>
        /// <returns>A function that given a <see cref="CancellationToken"/>, will get a waiter object.</returns>
        public static Func<CancellationToken, IWaiter> WaiterFactory { internal get; set; } =
            (cancellationToken) => new TaskDelayWaiter(cancellationToken);

        /// <summary>
        /// Create a <see cref="ResilientFunctionBuilder{TFunction, TResult}"/> from an existing async function with cancellation support. 
        /// </summary>
        /// <param name="function">An async function, that supports cancellation, that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientFunctionBuilder<Func<CancellationToken, Task<TResult>>, TResult> From<TResult>(Func<CancellationToken, Task<TResult>> function)
        {
            return new ResilientFunctionBuilder<Func<CancellationToken, Task<TResult>>, TResult>(function);
        }

        /// <summary>
        /// Create a <see cref="ResilientFunctionBuilder{TFunction, TResult}"/> from an existing async function. 
        /// </summary>
        /// <param name="function">An async function that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientFunctionBuilder<Func<Task<TResult>>, TResult> From<TResult>(Func<Task<TResult>> function)
        {
            return new ResilientFunctionBuilder<Func<Task<TResult>>, TResult>(function);
        }

        /// <summary>
        /// Create a <see cref="ResilientFunctionBuilder{TFunction, TResult}"/> from an existing synchronous function. 
        /// </summary>
        /// <param name="function">A synchronous function that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientFunctionBuilder<Func<TResult>, TResult> From<TResult>(Func<TResult> function)
        {
            return new ResilientFunctionBuilder<Func<TResult>, TResult>(function);
        }
    }
}
