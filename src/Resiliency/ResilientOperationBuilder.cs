using Resiliency.BackoffStrategies;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    public abstract class ResilientOperationBuilder<TOperation>
    {
        protected readonly string ImplicitOperationKey;
        protected readonly TOperation Operation;

        internal ResilientOperationBuilder(TOperation operation, int sourceLineNumber, string sourceFilePath, string memberName)
        {
            Handlers = new List<Func<ResilientOperation, Exception, Task<HandlerResult>>>();
            Operation = operation;
            ImplicitOperationKey = BuildImplicitOperationKey(sourceLineNumber, sourceFilePath, memberName);
            TimeoutPeriod = Timeout.InfiniteTimeSpan;
        }

        protected List<Func<ResilientOperation, Exception, Task<HandlerResult>>> Handlers { get; }
        protected TimeSpan TimeoutPeriod { get; private set; }

        private static string BuildImplicitOperationKey(int sourceLineNumber, string sourceFilePath, string memberName)
        {
            return $"{sourceFilePath}:{sourceLineNumber}_{memberName}";
        }

        protected void WhenExceptionIs<TException>(
            Func<ResilientOperation, TException, Task> handler)
            where TException : Exception
        {
            WhenExceptionIs(ex => true, handler);
        }

        protected void WhenExceptionIs<TException>(
            IBackoffStrategy backoffStrategy,
            Func<ResilientOperation, TException, Task> handler)
            where TException : Exception
        {
            WhenExceptionIs(ex => true, backoffStrategy, handler);
        }

        protected void WhenExceptionIs<TException>(
            Func<TException, bool> condition,
            Func<ResilientOperation, TException, Task> handler)
            where TException : Exception
        {
            Handlers.Add(async (op, ex) =>
            {
                op.Result = HandlerResult.Unhandled;

                if (ex is TException exception)
                {
                    if (condition(exception))
                    {
                        await handler(op, exception).ConfigureAwait(false);

                        if (op.Result == HandlerResult.Handled)
                        {
                            op.Handler._attemptsExhausted++;
                            op.Total._attemptsExhausted++;
                        }
                    }
                }

                return op.Result;
            });
        }

        protected void WhenExceptionIs<TException>(
            Func<TException, bool> condition,
            IBackoffStrategy backoffStrategy,
            Func<ResilientOperation, TException, Task> handler)
            where TException : Exception
        {
            Handlers.Add(async (op, ex) =>
            {
                op.Result = HandlerResult.Unhandled;

                if (ex is TException exception)
                {
                    if (condition(exception))
                    {
                        await handler(
                            ResilientOperationWithBackoff.FromResilientOperation(op, backoffStrategy),
                            exception).ConfigureAwait(false);

                        if (op.Result == HandlerResult.Handled)
                        {
                            op.Handler._attemptsExhausted++;
                            op.Total._attemptsExhausted++;
                        }
                    }
                }

                return op.Result;
            });
        }

        protected void TimeoutAfter(TimeSpan period)
        {
            TimeoutPeriod = period;
        }

        protected async Task ProcessHandlers(
            IEnumerable<Func<Exception, Task<HandlerResult>>> handlers,
            Exception ex,
            CancellationToken cancellationToken)
        {
            HandlerResult handlerResult = HandlerResult.Unhandled;

            foreach (var handler in handlers)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                handlerResult = await handler(ex).ConfigureAwait(false);
            }

            if (handlerResult != HandlerResult.Handled)
                ExceptionDispatchInfo.Capture(ex).Throw();
        }
    }
}
