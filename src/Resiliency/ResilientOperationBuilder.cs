using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    public abstract class ResilientOperationBuilder<TOperation>
    {
        private RetryTotalInfo Total;

        internal ResilientOperationBuilder(TOperation operation)
        {
            Handlers = new List<Func<Exception, Task<RetryHandlerResult>>>();
            Total = new RetryTotalInfo();
            Operation = operation;
        }

        protected List<Func<Exception, Task<RetryHandlerResult>>> Handlers { get; }

        protected TimeSpan TimeoutPeriod = Timeout.InfiniteTimeSpan;
        protected TOperation Operation { get; }

        protected void RetryWhenExceptionIs<TException>(
            Func<RetryOperation, TException, Task<RetryHandlerResult>> handler)
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
        }

        protected void RetryWhen(
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
        }

        protected void TimeoutAfter(TimeSpan period)
        {
            TimeoutPeriod = period;
        }

        protected async Task ProcessHandlers(Exception ex, CancellationToken cancellationToken)
        {
            RetryHandlerResult handlerResult = RetryHandlerResult.Unhandled;

            foreach (var handler in Handlers)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                handlerResult = await handler(ex).ConfigureAwait(false);
            }

            if (handlerResult != RetryHandlerResult.Handled)
                ExceptionDispatchInfo.Capture(ex).Throw();
        }
    }

    public class ResilientActionBuilder<TAction>
        : ResilientOperationBuilder<TAction>
    {
        internal ResilientActionBuilder(TAction action) 
            : base(action)
        {
        }

        public new ResilientActionBuilder<TAction> RetryWhenExceptionIs<TException>(
            Func<RetryOperation, TException, Task<RetryHandlerResult>> handler)
            where TException : Exception
        {
            base.RetryWhenExceptionIs(handler);

            return this;
        }

        public new ResilientActionBuilder<TAction> RetryWhen(
            Func<Exception, bool> condition,
            Func<RetryOperation, Exception, Task<RetryHandlerResult>> handler)
        {
            base.RetryWhen(condition, handler);

            return this;
        }

        public new ResilientActionBuilder<TAction> TimeoutAfter(TimeSpan period)
        {
            base.TimeoutAfter(period);

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

                    if (TimeoutPeriod != Timeout.InfiniteTimeSpan)
                    {
                        var taskSet = new List<Task>();
                        Task delayTask = Task.Delay(TimeoutPeriod);
                        taskSet.Add(delayTask);

                        var firstCompletedTask = await Task.WhenAny(taskSet).ConfigureAwait(false);

                        if (firstCompletedTask != operationTask)
                            throw new TimeoutException();
                    }
                    else
                    {
                        await operationTask.ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    await ProcessHandlers(ex, cancellationToken);
                }
            }
            while (true);
        }
    }

    public class ResilientFunctionBuilder<TFunction, TResult>
        : ResilientOperationBuilder<TFunction>
    {
        internal ResilientFunctionBuilder(TFunction function)
            : base(function)
        {
        }

        public new ResilientFunctionBuilder<TFunction, TResult> RetryWhenExceptionIs<TException>(
            Func<RetryOperation, TException, Task<RetryHandlerResult>> handler)
            where TException : Exception
        {
            base.RetryWhenExceptionIs(handler);

            return this;
        }

        public new ResilientFunctionBuilder<TFunction, TResult> RetryWhen(
            Func<Exception, bool> condition,
            Func<RetryOperation, Exception, Task<RetryHandlerResult>> handler)
        {
            base.RetryWhen(condition, handler);

            return this;
        }

        public new ResilientFunctionBuilder<TFunction, TResult> TimeoutAfter(TimeSpan period)
        {
            base.TimeoutAfter(period);

            return this;
        }

        public Func<CancellationToken, Task<TResult>> GetOperation()
        {
            return InvokeAsync;
        }

        public async Task<TResult> InvokeAsync(CancellationToken cancellationToken = default)
        {
            do
            {
                try
                {
                    Task<TResult> operationTask;

                    switch (Operation)
                    {
                        case Func<CancellationToken, Task<TResult>> asyncCancellableOperation:
                            operationTask = asyncCancellableOperation(cancellationToken);
                            break;
                        case Func<Task<TResult>> asyncOperation:
                            operationTask = asyncOperation();
                            break;
                        case Func<TResult> syncOperation:
                            operationTask = Task.Run(() => syncOperation());
                            break;
                        default:
                            throw new NotSupportedException($"Operation of type {Operation.GetType()}");
                    }

                    if (TimeoutPeriod != Timeout.InfiniteTimeSpan)
                    {
                        var taskSet = new List<Task>();
                        Task delayTask = Task.Delay(TimeoutPeriod);
                        taskSet.Add(delayTask);

                        var firstCompletedTask = await Task.WhenAny(taskSet).ConfigureAwait(false);

                        if (firstCompletedTask != operationTask)
                            throw new TimeoutException();

                        return operationTask.Result;
                    }
                    else
                    {
                        return await operationTask.ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    await ProcessHandlers(ex, cancellationToken);
                }
            }
            while (true);
        }
    }
}
