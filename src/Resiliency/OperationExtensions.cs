using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    public static class OperationExtensions
    {
        /// <summary>
        /// Create a <see cref="ResilientActionBuilder"/> from an existing async action with cancellation support. 
        /// </summary>
        /// <param name="action">An async action, that supports cancellation, that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientActionBuilder AsResilient(
            this Func<CancellationToken, Task> action,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "")
        {
            return ResilientOperation.From(action, sourceLineNumber, sourceFilePath, memberName);
        }

        /// <summary>
        /// Create a <see cref="ResilientActionBuilder"/> from an existing async action. 
        /// </summary>
        /// <param name="action">An async action that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientActionBuilder AsResilient(
            this Func<Task> action,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "")
        {
            return ResilientOperation.From(action, sourceLineNumber, sourceFilePath, memberName);
        }

        /// <summary>
        /// Create a <see cref="ResilientActionBuilder"/> from an existing synchronous action. 
        /// </summary>
        /// <param name="action">A synchronous action that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientActionBuilder AsResilient(
            this Action action,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "")
        {
            return ResilientOperation.From(action, sourceLineNumber, sourceFilePath, memberName);
        }

        /// <summary>
        /// Create a <see cref="ResilientFuncBuilder{TFunction, TResult}"/> from an existing async function with cancellation support. 
        /// </summary>
        /// <param name="function">An async function, that supports cancellation, that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientFuncBuilder<Func<CancellationToken, Task<TResult>>, TResult> AsResilient<TResult>(
            this Func<CancellationToken, Task<TResult>> function,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "")
        {
            return ResilientOperation.From(function, sourceLineNumber, sourceFilePath, memberName);
        }

        /// <summary>
        /// Create a <see cref="ResilientFuncBuilder{TFunction, TResult}"/> from an existing async function. 
        /// </summary>
        /// <param name="function">An async function that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientFuncBuilder<Func<Task<TResult>>, TResult> AsResilient<TResult>(
            this Func<Task<TResult>> function,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "")
        {
            return ResilientOperation.From(function, sourceLineNumber, sourceFilePath, memberName);
        }

        /// <summary>
        /// Create a <see cref="ResilientFuncBuilder{TFunction, TResult}"/> from an existing synchronous function. 
        /// </summary>
        /// <param name="function">A synchronous function that is not already resilient.</param>
        /// <returns>A new builder for the operation to configure resiliency.</returns>
        public static ResilientFuncBuilder<Func<TResult>, TResult> AsResilient<TResult>(
            this Func<TResult> function,
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "")
        {
            return ResilientOperation.From(function, sourceLineNumber, sourceFilePath, memberName);
        }
    }
}
