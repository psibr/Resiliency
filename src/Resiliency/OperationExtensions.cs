using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    public static class OperationExtensions
    {
        /// <summary>
        /// Create a <see cref="ResilientActionBuilder{TAction}"/> from an existing async action with cancellation support. 
        /// </summary>
        /// <param name="action">An async action, that supports cancellation, that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientActionBuilder<Func<CancellationToken, Task>> AsResilient(this Func<CancellationToken, Task> action) =>
            ResilientOperation.From(action);

        /// <summary>
        /// Create a <see cref="ResilientActionBuilder{TAction}"/> from an existing async action. 
        /// </summary>
        /// <param name="action">An async action that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientActionBuilder<Func<Task>> AsResilient(this Func<Task> action) =>
            ResilientOperation.From(action);

        /// <summary>
        /// Create a <see cref="ResilientActionBuilder{TAction}"/> from an existing synchronous action. 
        /// </summary>
        /// <param name="action">A synchronous action that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientActionBuilder<Action> AsResilient(this Action action) =>
            ResilientOperation.From(action);

        /// <summary>
        /// Create a <see cref="ResilientFunctionBuilder{TFunction, TResult}"/> from an existing async function with cancellation support. 
        /// </summary>
        /// <param name="function">An async function, that supports cancellation, that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientFunctionBuilder<Func<CancellationToken, Task<TResult>>, TResult> AsResilient<TResult>(
            this Func<CancellationToken, Task<TResult>> function) => ResilientOperation.From(function);

        /// <summary>
        /// Create a <see cref="ResilientFunctionBuilder{TFunction, TResult}"/> from an existing async function. 
        /// </summary>
        /// <param name="function">An async function that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientFunctionBuilder<Func<Task<TResult>>, TResult> AsResilient<TResult>(this Func<Task<TResult>> function) =>
            ResilientOperation.From(function);

        /// <summary>
        /// Create a <see cref="ResilientFunctionBuilder{TFunction, TResult}"/> from an existing synchronous function. 
        /// </summary>
        /// <param name="function">A synchronous function that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientFunctionBuilder<Func<TResult>, TResult> AsResilient<TResult>(this Func<TResult> function) =>
            ResilientOperation.From(function);
    }
}
