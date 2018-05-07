using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace REtry
{
    public class RetryOperationBuilder<TOperation>
    {
        private RetryTotalInfo Total;

        internal RetryOperationBuilder(TOperation operation)
        {
            Handlers = new List<Func<Exception, Task<bool>>>();
            Total = new RetryTotalInfo();
            Operation = operation;
        }

        private List<Func<Exception, Task<bool>>> Handlers { get; set; }
        private TOperation Operation { get; }

        public RetryOperationBuilder<TOperation> WhenExceptionIs<TException>(Func<IRetryOperation, TException, Task<bool>> handler)
            where TException : Exception
        {
            var info = new RetryHandlerInfo();

            var operationInfo = Retry.RetryOperationFactory(info, Total);

            Handlers.Add(async (ex) =>
            {
                bool didHandle = false;

                if (ex is TException exception)
                {
                    didHandle = await handler(operationInfo, exception).ConfigureAwait(false);

                    if (didHandle)
                    {
                        operationInfo.Handler.AttemptsExhausted++;
                        operationInfo.Total.AttemptsExhausted++;
                    }

                    return didHandle;
                }

                return didHandle;
            });

            return this;
        }

        public RetryOperationBuilder<TOperation> When(Func<Exception, bool> condition, Func<IRetryOperation, Exception, Task<bool>> handler)
        {
            var info = new RetryHandlerInfo();

            var operationInfo = new RetryOperation(info, Total);

            Handlers.Add(async (ex) =>
            {
                bool didHandle = false;

                if (condition(ex))
                {
                    didHandle = await handler(operationInfo, ex).ConfigureAwait(false);

                    if (didHandle)
                    {
                        operationInfo.Handler.AttemptsExhausted++;
                        operationInfo.Total.AttemptsExhausted++;
                    }

                    return didHandle;
                }

                return didHandle;
            });

            return this;
        }

        public Func<CancellationToken, Task> GetRetryableOperation()
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
                    bool wasHandled = false;

                    foreach (var handler in Handlers)
                    {
                        wasHandled = await handler(ex).ConfigureAwait(false);
                    }

                    if (!wasHandled)
                        throw;
                }
            }
            while (true);
        }
    }
}
