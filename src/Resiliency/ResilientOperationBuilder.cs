using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
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

        private TimeSpan TimeoutPeriod = Timeout.InfiniteTimeSpan;
        private TOperation Operation { get; }

        public ResilientOperationBuilder<TOperation> RetryWhenExceptionIs<TException>(Func<RetryOperation, TException, Task<RetryHandlerResult>> handler)
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

        public ResilientOperationBuilder<TOperation> RetryWhen(
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

        public ResilientOperationBuilder<TOperation> TimeoutAfter(TimeSpan period)
        {
            TimeoutPeriod = period;

            return this;
        }

        public Func<CancellationToken, Task> GetOperation()
        {
            return InvokeAsync;
        }

        public async Task InvokeAsync(CancellationToken cancellationToken = default)
        {
            do
            {
                try
                {
                    var taskSet = new List<Task>();
                    Task operationTask;
                    
                    switch (Operation)
                    {
                        case Func<CancellationToken, Task> asyncCancellableOperation:
                            operationTask = asyncCancellableOperation(cancellationToken);
                            break;
                        case Func<Task> asyncOperation:
                            operationTask = asyncOperation();
                            break;
                        case Action syncOperation:
                            operationTask = Task.Run(() => syncOperation());
                            break;
                        default:
                            throw new NotSupportedException($"Operation of type {Operation.GetType()}");
                    }

                    taskSet.Add(operationTask);

                    if(TimeoutPeriod != Timeout.InfiniteTimeSpan)
                        taskSet.Add(Task.Delay(TimeoutPeriod));

                    var firstCompletedTask = await Task.WhenAny(taskSet).ConfigureAwait(false);

                    if(firstCompletedTask != operationTask)
                        throw new TimeoutException();
                }
                catch (Exception ex)
                {
                    RetryHandlerResult handlerResult = RetryHandlerResult.Unhandled;

                    foreach (var handler in Handlers)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        handlerResult = await handler(ex).ConfigureAwait(false);
                    }

                    if (handlerResult != RetryHandlerResult.Handled)
                        throw;
                }
            }
            while (true);
        }
    }
}
