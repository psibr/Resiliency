using Resiliency.BackoffStrategies;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    public abstract class ResilientOperationBuilder<TOperation, TResilientOperation, TResult>
        where TResilientOperation : ResilientOperation<TResult>
    {
        protected readonly string ImplicitOperationKey;
        protected readonly TOperation Operation;

        internal ResilientOperationBuilder(TOperation operation, int sourceLineNumber, string sourceFilePath, string memberName)
        {
            Handlers = new List<Func<TResilientOperation, Exception, Task<ResilientOperation<TResult>>>>();
            Operation = operation;
            ImplicitOperationKey = BuildImplicitOperationKey(sourceLineNumber, sourceFilePath, memberName);
            TimeoutPeriod = Timeout.InfiniteTimeSpan;
        }

        protected List<Func<TResilientOperation, Exception, Task<ResilientOperation<TResult>>>> Handlers { get; }
        protected TimeSpan TimeoutPeriod { get; private set; }

        private static string BuildImplicitOperationKey(int sourceLineNumber, string sourceFilePath, string memberName)
        {
            return $"{sourceFilePath}:{sourceLineNumber}_{memberName}";
        }

        protected void WhenExceptionIs<TException>(
            Func<TResilientOperation, TException, Task> handler)
            where TException : Exception
        {
            WhenExceptionIs(ex => true, handler);
        }

        protected void WhenExceptionIs<TException>(
            IBackoffStrategy backoffStrategy,
            Func<TResilientOperation, TException, Task> handler)
            where TException : Exception
        {
            WhenExceptionIs(ex => true, backoffStrategy, handler);
        }

        protected void WhenExceptionIs<TException>(
            Func<TException, bool> condition,
            Func<TResilientOperation, TException, Task> handler)
            where TException : Exception
        {
            Handlers.Add(async (op, ex) =>
            {
                op.HandlerResult = HandlerResult.Unhandled;

                if (ex is TException exception)
                {
                    if (condition(exception))
                    {
                        await handler(op, exception).ConfigureAwait(false);

                        if (op.HandlerResult == HandlerResult.Retry)
                        {
                            op.Handler._attemptsExhausted++;
                            op.Total._attemptsExhausted++;
                        }
                    }
                }

                return op;
            });
        }

        protected void WhenExceptionIs<TException>(
            Func<TException, bool> condition,
            IBackoffStrategy backoffStrategy,
            Func<TResilientOperation, TException, Task> handler)
            where TException : Exception
        {
            Handlers.Add(async (op, ex) =>
            {
                op.HandlerResult = HandlerResult.Unhandled;

                if (ex is TException exception)
                {
                    if (condition(exception))
                    {
                        op.BackoffStrategy = backoffStrategy;

                        await handler(
                            op,
                            exception).ConfigureAwait(false);

                        if (op.HandlerResult == HandlerResult.Retry)
                        {
                            op.Handler._attemptsExhausted++;
                            op.Total._attemptsExhausted++;
                        }
                    }
                }

                return op;
            });
        }

        protected void TimeoutAfter(TimeSpan period)
        {
            TimeoutPeriod = period;
        }
    }
}
