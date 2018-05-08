using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace REtry
{
    public class ResilientOperationBuilder<TOperation>
    {
        private RetryTotalInfo Total;

        internal ResilientOperationBuilder(TOperation operation)
        {
            Handlers = new List<Func<Exception, Task<RetryHandlerResult>>>();
            Total = new RetryTotalInfo();
            Operation = operation;
        }

        private List<Func<Exception, Task<RetryHandlerResult>>> Handlers { get; }
        private TOperation Operation { get; }

        public ResilientOperationBuilder<TOperation> WhenExceptionIs<TException>(Func<RetryOperation, TException, Task<RetryHandlerResult>> handler)
            where TException : Exception
        {
            var info = new RetryHandlerInfo();

            var operationInfo = new RetryOperation(info, Total);

            Handlers.Add(async (ex) =>
            {
                var handlerResult = RetryHandlerResult.Unhandled;

                if (ex is TException exception)
                {
                    handlerResult = await handler(operationInfo, exception).ConfigureAwait(false);

                    switch (handlerResult)
                    {
                        case RetryHandlerResult.Handled:
                            operationInfo.Handler.AttemptsExhausted++;
                            operationInfo.Total.AttemptsExhausted++;
                            break;
                    }
                }

                return handlerResult;
            });

            return this;
        }

        public ResilientOperationBuilder<TOperation> When(
            Func<Exception, bool> condition,
            Func<RetryOperation, Exception, Task<RetryHandlerResult>> handler)
        {
            var info = new RetryHandlerInfo();

            var operationInfo = new RetryOperation(info, Total);

            Handlers.Add(async (ex) =>
            {
                RetryHandlerResult handlerResult = RetryHandlerResult.Unhandled;

                if (condition(ex))
                {
                    handlerResult = await handler(operationInfo, ex).ConfigureAwait(false);

                    switch (handlerResult)
                    {
                        case RetryHandlerResult.Handled:
                            operationInfo.Handler.AttemptsExhausted++;
                            operationInfo.Total.AttemptsExhausted++;
                            break;
                    }
                }

                return handlerResult;
            });

            return this;
        }

        public Func<CancellationToken, Task> GetResilientOperation()
        {
            return Invoke;
        }

        public async Task Invoke(CancellationToken cancellationToken = default)
        {
            do
            {
                try
                {
                    switch (Operation)
                    {
                        case Func<CancellationToken, Task> asyncCancellableOperation:
                            await asyncCancellableOperation(cancellationToken).ConfigureAwait(false);
                            break;
                        case Func<Task> asyncOperation:
                            await asyncOperation().ConfigureAwait(false);
                            break;
                        case Action syncOperation:
                            syncOperation();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    RetryHandlerResult handlerResult = RetryHandlerResult.Unhandled;

                    foreach (var handler in Handlers)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        handlerResult = await handler(ex).ConfigureAwait(false);

                        if (handlerResult == RetryHandlerResult.Cancelled)
                            break;
                    }

                    if (handlerResult != RetryHandlerResult.Handled)
                        throw;
                }
            }
            while (true);
        }
    }
}
