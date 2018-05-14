using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resiliency
{
    public class ResilientFuncBuilder<TFunc, TResult>
        : ResilientOperationBuilder<TFunc>
    {
        protected List<Func<Func<CancellationToken, Task<TResult>>, Func<CancellationToken, Task<TResult>>>> CircuitBreakerHandlers
            = new List<Func<Func<CancellationToken, Task<TResult>>, Func<CancellationToken, Task<TResult>>>>();

        internal ResilientFuncBuilder(TFunc function, int sourceLineNumber, string sourceFilePath, string memberName)
            : base(function, sourceLineNumber, sourceFilePath, memberName)
        {
            // Add an implicit operation circuit breaker that must be explicitly tripped.
            // This is comonly used for 429 and 503 style exceptions where you KNOW you have been throttled.
            WithCircuitBreaker(ImplicitOperationKey, () => new CircuitBreaker(new ExplicitCircuitBreakerStrategy()));
        }

        public new ResilientFuncBuilder<TFunc, TResult> WhenExceptionIs<TException>(
            Func<ResilientOperation, TException, Task<HandlerResult>> handler)
            where TException : Exception
        {
            base.WhenExceptionIs(handler);

            return this;
        }

        public new ResilientFuncBuilder<TFunc, TResult> When(
            Func<Exception, bool> condition,
            Func<ResilientOperation, Exception, Task<HandlerResult>> handler)
        {
            base.When(condition, handler);

            return this;
        }

        public new ResilientFuncBuilder<TFunc, TResult> TimeoutAfter(TimeSpan period)
        {
            base.TimeoutAfter(period);

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

            do
            {
                try
                {
                    return await wrappedOperation(cancellationToken);
                }
                catch (Exception ex)
                {
                    try
                    {
                        await ProcessHandlers(ex, cancellationToken);
                    }
                    catch (CircuitBrokenException circuitBrokenEx)
                    {
                        await ProcessHandlers(circuitBrokenEx, cancellationToken);
                    }
                }
            }
            while (true);
        }

        private async Task<TResult> ExecuteFunc(CancellationToken cancellationToken)
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
    }
}