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
            Func<ResilientOperation, TException, Task<HandlerResult>> handler)
            where TException : Exception
        {
            Handlers.Add(async (op, ex) =>
            {
                var handlerResult = HandlerResult.Unhandled;

                if (ex is TException exception)
                {
                    handlerResult = await handler(op, exception).ConfigureAwait(false);

                    switch (handlerResult)
                    {
                        case HandlerResult.Handled:
                            op.Handler.AttemptsExhausted++;
                            op.Total.AttemptsExhausted++;
                            break;
                    }
                }

                return handlerResult;
            });
        }

        protected void When(
            Func<Exception, bool> condition,
            Func<ResilientOperation, Exception, Task<HandlerResult>> handler)
        {
            Handlers.Add(async (op, ex) =>
            {
                HandlerResult handlerResult = HandlerResult.Unhandled;

                if (condition(ex))
                {
                    handlerResult = await handler(op, ex).ConfigureAwait(false);

                    switch (handlerResult)
                    {
                        case HandlerResult.Handled:
                            op.Handler.AttemptsExhausted++;
                            op.Total.AttemptsExhausted++;
                            break;
                    }
                }

                return handlerResult;
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
