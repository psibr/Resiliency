using Resiliency.BackoffStrategies;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    public class ResilientFuncBuilder<TFunc, TResult>
    {
        protected readonly string ImplicitOperationKey;
        protected readonly TFunc Function;

        internal ResilientFuncBuilder(TFunc function, int sourceLineNumber, string sourceFilePath, string memberName)
        {
            Function = function;
            ImplicitOperationKey = BuildImplicitOperationKey(sourceLineNumber, sourceFilePath, memberName);
            TimeoutPeriod = Timeout.InfiniteTimeSpan;

            Handlers = new List<Func<ResilientOperation<TResult>, Exception, Task<ResilientOperation<TResult>>>>();
            ResultHandlers = new List<Func<ResilientOperation<TResult>, TResult, Task<ResilientOperation<TResult>>>>();
            CircuitBreakerHandlers = new List<Func<Func<CancellationToken, Task<TResult>>, Func<CancellationToken, Task<TResult>>>>();

            // Add an implicit operation circuit breaker that must be explicitly tripped.
            // This is comonly used for 429 and 503 style exceptions where you KNOW you have been throttled.
            WithCircuitBreaker(ImplicitOperationKey, () => new CircuitBreaker(new ExplicitTripCircuitBreakerStrategy()));
        }

        protected List<Func<ResilientOperation<TResult>, Exception, Task<ResilientOperation<TResult>>>> Handlers { get; }

        protected List<Func<ResilientOperation<TResult>, TResult, Task<ResilientOperation<TResult>>>> ResultHandlers { get; }

        protected List<Func<Func<CancellationToken, Task<TResult>>, Func<CancellationToken, Task<TResult>>>> CircuitBreakerHandlers { get; }

        protected TimeSpan TimeoutPeriod { get; private set; }

        private static string BuildImplicitOperationKey(int sourceLineNumber, string sourceFilePath, string memberName)
        {
            return $"{sourceFilePath}:{sourceLineNumber}_{memberName}";
        }

        public ResilientFuncBuilder<TFunc, TResult> WhenExceptionIs<TException>(
            Func<IResilientOperation<TResult>, TException, Task> handler)
            where TException : Exception
        {
            WhenExceptionIs(ex => true, handler);

            return this;
        }

        public ResilientFuncBuilder<TFunc, TResult> WhenExceptionIs<TException>(
            IBackoffStrategy backoffStrategy,
            Func<IResilientOperationWithBackoff<TResult>, TException, Task> handler)
            where TException : Exception
        {
            WhenExceptionIs(ex => true, backoffStrategy, handler);

            return this;
        }

        public ResilientFuncBuilder<TFunc, TResult> WhenExceptionIs<TException>(
            Func<TException, bool> condition,
            Func<IResilientOperation<TResult>, TException, Task> handler)
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

            return this;
        }

        public ResilientFuncBuilder<TFunc, TResult> WhenExceptionIs<TException>(
            Func<TException, bool> condition,
            IBackoffStrategy backoffStrategy,
            Func<IResilientOperationWithBackoff<TResult>, TException, Task> handler)
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

            return this;
        }

        public ResilientFuncBuilder<TFunc, TResult> WhenResult(
            Func<TResult, bool> condition,
            Func<IResilientOperation<TResult>, TResult, Task> handler)
        {
            ResultHandlers.Add(async (op, result) =>
            {
                op.HandlerResult = HandlerResult.Unhandled;

                if (condition(result))
                {
                    await handler(op, result).ConfigureAwait(false);

                    if (op.HandlerResult == HandlerResult.Retry)
                    {
                        op.Handler._attemptsExhausted++;
                        op.Total._attemptsExhausted++;
                    }
                }

                return op;
            });

            return this;
        }

        public ResilientFuncBuilder<TFunc, TResult> WhenResult(
            Func<TResult, bool> condition,
            IBackoffStrategy backoffStrategy,
            Func<IResilientOperationWithBackoff<TResult>, TResult, Task> handler)
        {
            ResultHandlers.Add(async (op, result) =>
            {
                op.HandlerResult = HandlerResult.Unhandled;

                if (condition(result))
                {
                    op.BackoffStrategy = backoffStrategy;

                    await handler(
                        op,
                        result).ConfigureAwait(false);

                    if (op.HandlerResult == HandlerResult.Retry)
                    {
                        op.Handler._attemptsExhausted++;
                        op.Total._attemptsExhausted++;
                    }
                }

                return op;
            });

            return this;
        }

        public ResilientFuncBuilder<TFunc, TResult> TimeoutAfter(TimeSpan period)
        {
            TimeoutPeriod = period;

            return this;
        }

        public ResilientFuncBuilder<TFunc, TResult> WithCircuitBreaker(string operationKey, Func<CircuitBreaker> onMissingFactory)
        {
            var circuitBreaker = CircuitBreaker.GetOrAddCircuitBreaker(operationKey, onMissingFactory);

            return WithCircuitBreaker(circuitBreaker);
        }

        protected ResilientFuncBuilder<TFunc, TResult> WithCircuitBreaker(CircuitBreaker circuitBreaker)
        {
            CircuitBreakerHandlers.Add(WrapOperationWithCircuitBreaker);

            return this;

            Func<CancellationToken, Task<TResult>> WrapOperationWithCircuitBreaker(Func<CancellationToken, Task<TResult>> operation)
            {
                return async (cancellationToken) =>
                {
                    bool lockTaken = false;

                    switch (circuitBreaker.State)
                    {
                        case CircuitState.Open:

                            if (CooldownPeriodHasElapsed())
                            {
                                try
                                {
                                    Monitor.TryEnter(circuitBreaker.SyncRoot, ref lockTaken);
                                    if (lockTaken)
                                    {
                                        // Set the circuit breaker state to HalfOpen.
                                        circuitBreaker.TestCircuit();

                                        // Attempt the operation.
                                        return await AttemptHalfOpenExecution();
                                    }
                                }
                                finally
                                {
                                    if (lockTaken)
                                    {
                                        Monitor.Exit(circuitBreaker.SyncRoot);
                                    }
                                }
                            }

                            throw new CircuitBrokenException(circuitBreaker.LastException, (circuitBreaker.CooldownCompleteAt - DateTimeOffset.UtcNow).Duration());


                        case CircuitState.HalfOpen:

                            try
                            {
                                Monitor.TryEnter(circuitBreaker.SyncRoot, ref lockTaken);
                                if (lockTaken)
                                {
                                    // Attempt the operation.
                                    return await AttemptHalfOpenExecution();
                                }
                            }
                            finally
                            {
                                if (lockTaken)
                                {
                                    Monitor.Exit(circuitBreaker.SyncRoot);
                                }
                            }

                            throw new CircuitBrokenException(circuitBreaker.LastException, (circuitBreaker.CooldownCompleteAt - DateTimeOffset.UtcNow).Duration());


                        case CircuitState.Closed:
                            try
                            {
                                return await operation(cancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception ex) when (!(ex is CircuitBrokenException))
                            {
                                circuitBreaker.OnFailure(ex);

                                throw;
                            }

                        default:
                            throw new ArgumentOutOfRangeException(nameof(CircuitBreaker.State), $"Unhandled value of {nameof(CircuitState)}.");
                    }

                    async Task<TResult> AttemptHalfOpenExecution()
                    {
                        var result = await operation(cancellationToken).ConfigureAwait(false);

                        circuitBreaker.OnHalfOpenSuccess();

                        return result;
                    }

                    bool CooldownPeriodHasElapsed()
                    {
                        return circuitBreaker.CooldownCompleteAt <= DateTimeOffset.UtcNow;
                    }
                };
            }
        }

        public Func<CancellationToken, Task<TResult>> GetOperation()
        {
            return InvokeAsync;
        }

        public async Task<TResult> InvokeAsync(CancellationToken cancellationToken = default)
        {
            // Build wrapped operation
            Func<CancellationToken, Task<TResult>> wrappedOperation = ExecuteFunc;

            foreach (var circuitBreakerHandler in CircuitBreakerHandlers)
            {
                wrappedOperation = circuitBreakerHandler(wrappedOperation);
            }

            var totalInfo = new ResilientOperationTotalInfo();

            var partiallyAppliedResultHandlers = new List<Func<TResult, Task<ResilientOperation<TResult>>>>();

            // Prepare handlers
            foreach (var handler in ResultHandlers)
            {
                var op = new ResilientOperation<TResult>(ImplicitOperationKey, new ResilientOperationHandlerInfo(), totalInfo, cancellationToken);

                partiallyAppliedResultHandlers.Add(result => handler(op, result));
            }

            var partiallyAppliedHandlers = new List<Func<Exception, Task<ResilientOperation<TResult>>>>();

            // Prepare handlers
            foreach (var handler in Handlers)
            {
                var op = new ResilientOperation<TResult>(ImplicitOperationKey, new ResilientOperationHandlerInfo(), totalInfo, cancellationToken);

                partiallyAppliedHandlers.Add((ex) => handler(op, ex));
            }

            do
            {
                try
                {
                    var result = await wrappedOperation(cancellationToken);

                    ResilientOperation<TResult> op = null;

                    foreach (var handler in partiallyAppliedResultHandlers)
                    {
                        op = await handler(result).ConfigureAwait(false);

                        if (op.HandlerResult == HandlerResult.Unhandled)
                            continue;
                        else if (op.HandlerResult == HandlerResult.Retry)
                            break;
                        else if (op.HandlerResult == HandlerResult.Break)
                            return result;
                        else if (op.HandlerResult == HandlerResult.Return)
                            return op.Result;
                    }

                    if (op?.HandlerResult == HandlerResult.Retry)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        continue;
                    }

                    return result;
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    var exToHandle = ex;

                    ResilientOperation<TResult> op = null;

                    do
                    {
                        try
                        {
                            foreach (var handler in partiallyAppliedHandlers)
                            {
                                op = await handler(exToHandle).ConfigureAwait(false);

                                if (op.HandlerResult == HandlerResult.Unhandled)
                                    continue;
                                else if (op.HandlerResult == HandlerResult.Retry)
                                    break;
                                else if (op.HandlerResult == HandlerResult.Break)
                                    break;
                                else if (op.HandlerResult == HandlerResult.Return)
                                    return op.Result;
                            }

                            if (op?.HandlerResult == HandlerResult.Retry)
                                break;

                            ExceptionDispatchInfo.Capture(exToHandle).Throw();
                        }
                        catch (Exception handlerEx) when (handlerEx != exToHandle && !(handlerEx is OperationCanceledException))
                        {
                            exToHandle = handlerEx;

                            continue;
                        }
                    }
                    while (true);

                    continue;
                }
            }
            while (true);
        }

        private async Task<TResult> ExecuteFunc(CancellationToken cancellationToken)
        {
            var timeoutInclusiveCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutOrCancelledToken = timeoutInclusiveCancellationSource.Token;

            Task<TResult> operationTask;

            switch (Function)
            {
                case Func<CancellationToken, Task<TResult>> asyncCancellableOperation:
                    operationTask = asyncCancellableOperation(timeoutOrCancelledToken);
                    break;
                case Func<Task<TResult>> asyncOperation:
                    operationTask = asyncOperation();
                    break;
                case Func<TResult> syncOperation:
                    operationTask = Task.Run(() => syncOperation(), timeoutOrCancelledToken);
                    break;
                default:
                    throw new NotSupportedException($"Operation of type {Function.GetType()} not supported.");
            }

            if (TimeoutPeriod != Timeout.InfiniteTimeSpan)
            {
                var taskSet = new List<Task>();
                var delayTask = Task.Delay(TimeoutPeriod, timeoutOrCancelledToken);
                taskSet.Add(delayTask);

                var firstCompletedTask = await Task.WhenAny(taskSet).ConfigureAwait(false);

                if (firstCompletedTask != operationTask)
                {
                    // Signal cancellation as a best practice to end the actual operation, but don't wait or observe it.
                    timeoutInclusiveCancellationSource.Cancel();

                    throw new TimeoutException();
                }

                return operationTask.Result;
            }
            else
            {
                return await operationTask.ConfigureAwait(false);
            }
        }
    }
}