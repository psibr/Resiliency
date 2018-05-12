using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Resiliency
{
    public enum CircuitState
    {
        Closed = 0,
        HalfOpen = 1,
        Open = 2
    }

    public class CircuitBrokenException
        : Exception
    {
        public CircuitBrokenException(TimeSpan remainingCooldownPeriod)
            : base()
        {
            RemainingCooldownPeriod = remainingCooldownPeriod;
        }

        public CircuitBrokenException(TimeSpan remainingCooldownPeriod, string message)
            : base(message)
        {
            RemainingCooldownPeriod = remainingCooldownPeriod;
        }

        public CircuitBrokenException(TimeSpan remainingCooldownPeriod, string message, Exception innerException)
            : base(message, innerException)
        {
            RemainingCooldownPeriod = remainingCooldownPeriod;
        }

        public TimeSpan RemainingCooldownPeriod { get; }
    }

    public interface ICircuitBreakerStateStore
    {
        CircuitBreakerStateEnum State { get; }

        Exception LastException { get; }

        DateTime LastStateChangedDateUtc { get; }

        void Trip(Exception ex);

        void Reset();

        void HalfOpen();

        bool IsClosed { get; }
    }

    public class CircuitBreaker
    {
        public CircuitBreaker(CircuitState initialState = CircuitState.Closed)
        {
            State = initialState;
        }

        public CircuitState State { get; }

        public DateTimeOffset? ResumeTime { get; }

        private object SyncRoot = new object();

        private SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

        public void Trip(TimeSpan cooldownPeriod)
        {
            if (State == CircuitState.Open)
                throw new CircuitBrokenException(ResumeTime - DateTimeOffset.UtcNow);

            lock (SyncRoot)
            {
                if (State == CircuitState.Open)
                    throw new CircuitBrokenException(ResumeTime - DateTimeOffset.UtcNow);

                State = CircuitState.Open;
                ResumeTime = DateTimeOffset.UtcNow + cooldownPeriod;

                throw new CircuitBrokenException(ResumeTime - DateTimeOffset.UtcNow)
                }
        }

        public static CircuitBreaker GetCircuitBreakerFromKey<TKey>(TKey key)
        {
            return CircuitBreakers.GetOrAdd(key, () => new CircuitBreaker());
        }

        private static ConcurrentDictionary<object, CircuitBreaker> CircuitBreakers = new ConcurrentDictionary<object, CircuitBreaker>();
    }

    public abstract class ResilientOperationBuilder<TOperation>
    {
        private ResilientOperationTotalInfo Total;

        internal ResilientOperationBuilder(TOperation operation)
        {
            Handlers = new List<Func<Exception, Task<HandlerResult>>>();
            Total = new ResilientOperationTotalInfo();
            Operation = operation;
            TimeoutPeriod = Timeout.InfiniteTimeSpan;
        }

        protected List<Func<Exception, Task<HandlerResult>>> Handlers { get; }

        protected TimeSpan TimeoutPeriod;
        protected TOperation Operation { get; }

        protected void WhenExceptionIs<TException>(
            Func<ResilientOperation, TException, Task<HandlerResult>> handler)
            where TException : Exception
        {
            var info = new ResilientOperationHandlerInfo();

            var operationInfo = new ResilientOperation(info, Total);

            Handlers.Add(async (ex) =>
            {
                var handlerResult = HandlerResult.Unhandled;

                if (ex is TException exception)
                {
                    handlerResult = await handler(operationInfo, exception).ConfigureAwait(false);

                    switch (handlerResult)
                    {
                        case HandlerResult.Handled:
                            operationInfo.Handler.AttemptsExhausted++;
                            operationInfo.Total.AttemptsExhausted++;
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
            var info = new ResilientOperationHandlerInfo();

            var operationInfo = new ResilientOperation(info, Total);

            Handlers.Add(async (ex) =>
            {
                HandlerResult handlerResult = HandlerResult.Unhandled;

                if (condition(ex))
                {
                    handlerResult = await handler(operationInfo, ex).ConfigureAwait(false);

                    switch (handlerResult)
                    {
                        case HandlerResult.Handled:
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
            HandlerResult handlerResult = HandlerResult.Unhandled;

            foreach (var handler in Handlers)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                handlerResult = await handler(ex).ConfigureAwait(false);
            }

            if (handlerResult != HandlerResult.Handled)
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

        public new ResilientActionBuilder<TAction> WhenExceptionIs<TException>(
            Func<ResilientOperation, TException, Task<HandlerResult>> handler)
            where TException : Exception
        {
            base.WhenExceptionIs(handler);

            return this;
        }

        public new ResilientActionBuilder<TAction> When(
            Func<Exception, bool> condition,
            Func<ResilientOperation, Exception, Task<HandlerResult>> handler)
        {
            base.When(condition, handler);

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

        public new ResilientFunctionBuilder<TFunction, TResult> WhenExceptionIs<TException>(
            Func<ResilientOperation, TException, Task<HandlerResult>> handler)
            where TException : Exception
        {
            base.WhenExceptionIs(handler);

            return this;
        }

        public new ResilientFunctionBuilder<TFunction, TResult> When(
            Func<Exception, bool> condition,
            Func<ResilientOperation, Exception, Task<HandlerResult>> handler)
        {
            base.When(condition, handler);

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
