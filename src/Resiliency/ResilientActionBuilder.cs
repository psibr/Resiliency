using Resiliency.BackoffStrategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ActionOperation = System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task>;

namespace Resiliency
{

    using ActionOperationWrapper = Func<ActionOperation, ActionOperation>;

    public class Unit : IEquatable<Unit>
    {
        public bool Equals(Unit other) => true;

        public override bool Equals(object obj) => obj is Unit;

        public override int GetHashCode() => 0;

        public override string ToString() => "()";
    }

    public class ResilientActionBuilder<TAction>
        : ResilientOperationBuilder<TAction, ResilientOperation<Unit>, Unit>
    {
        protected List<ActionOperationWrapper> CircuitBreakerHandlers = new List<ActionOperationWrapper>();

        internal ResilientActionBuilder(TAction action, int sourceLineNumber, string sourceFilePath, string memberName)
            : base(action, sourceLineNumber, sourceFilePath, memberName)
        {
            // Add an implicit operation circuit breaker that must be explicitly tripped.
            // This is comonly used for 429 and 503 style exceptions where you KNOW you have been throttled.
            WithCircuitBreaker(ImplicitOperationKey, () => new CircuitBreaker(new ExplicitTripCircuitBreakerStrategy()));
        }

        public ResilientActionBuilder<TAction> WhenExceptionIs<TException>(
            Func<IResilientOperation, TException, Task> handler)
            where TException : Exception
        {
            base.WhenExceptionIs(handler);

            return this;
        }

        public ResilientActionBuilder<TAction> WhenExceptionIs<TException>(
            IBackoffStrategy backoffStrategy,
            Func<IResilientOperationWithBackoff, TException, Task> handler)
            where TException : Exception
        {
            base.WhenExceptionIs(backoffStrategy, handler);

            return this;
        }

        public ResilientActionBuilder<TAction> WhenExceptionIs<TException>(
            Func<TException, bool> condition,
            Func<IResilientOperation, TException, Task> handler)
            where TException : Exception
        {
            base.WhenExceptionIs(condition, handler);

            return this;
        }

        public ResilientActionBuilder<TAction> WhenExceptionIs<TException>(
            Func<TException, bool> condition,
            IBackoffStrategy backoffStrategy,
            Func<IResilientOperationWithBackoff, TException, Task> handler)
            where TException : Exception
        {
            base.WhenExceptionIs(condition, backoffStrategy, handler);

            return this;
        }

        public new ResilientActionBuilder<TAction> TimeoutAfter(TimeSpan period)
        {
            base.TimeoutAfter(period);

            return this;
        }

        public ActionOperation GetOperation()
        {
            return InvokeAsync;
        }

        public ResilientActionBuilder<TAction> WithCircuitBreaker(string operationKey, Func<CircuitBreaker> onMissingFactory)
        {
            var circuitBreaker = CircuitBreaker.GetOrAddCircuitBreaker(operationKey, onMissingFactory);

            return WithCircuitBreaker(circuitBreaker);
        }

        protected ResilientActionBuilder<TAction> WithCircuitBreaker(CircuitBreaker circuitBreaker)
        {
            CircuitBreakerHandlers.Add(WrapOperationWithCircuitBreaker);

            return this;

            ActionOperation WrapOperationWithCircuitBreaker(ActionOperation operation)
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
                                        await AttemptHalfOpenExecution();
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
                                    await AttemptHalfOpenExecution();
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
                                await operation(cancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception ex) when (!(ex is CircuitBrokenException))
                            {
                                circuitBreaker.OnFailure(ex);

                                throw;
                            }
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(CircuitBreaker.State), $"Unhandled value of {nameof(CircuitState)}.");
                    }

                    async Task AttemptHalfOpenExecution()
                    {
                        await operation(cancellationToken).ConfigureAwait(false);

                        circuitBreaker.OnHalfOpenSuccess();
                    }

                    bool CooldownPeriodHasElapsed()
                    {
                        return circuitBreaker.CooldownCompleteAt <= DateTimeOffset.UtcNow;
                    }
                };
            }
        }

        public async Task InvokeAsync(CancellationToken cancellationToken = default)
        {
            // Build wrapped operation
            ActionOperation wrappedOperation = ExecuteAction;

            foreach (var circuitBreakerHandler in CircuitBreakerHandlers)
            {
                wrappedOperation = circuitBreakerHandler(wrappedOperation);
            }

            var totalInfo = new ResilientOperationTotalInfo();

            var partiallyAppliedHandlers = new List<Func<Exception, Task<ResilientOperation<Unit>>>>();

            // Prepare handlers
            foreach (var handler in Handlers)
            {
                var op = new ResilientOperation<Unit>(ImplicitOperationKey, new ResilientOperationHandlerInfo(), totalInfo, cancellationToken);

                partiallyAppliedHandlers.Add((ex) => handler(op, ex));
            }

            do
            {
                try
                {
                    await wrappedOperation(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    var exToHandle = ex;

                    ResilientOperation<Unit> op = null;

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
                                    return;
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

        protected virtual async Task ExecuteAction(CancellationToken cancellationToken)
        {
            var timeoutInclusiveCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutOrCancelledToken = timeoutInclusiveCancellationSource.Token;

            Task operationTask;

            switch (Operation)
            {
                case Func<CancellationToken, Task> asyncCancellableOperation:
                    operationTask = asyncCancellableOperation(timeoutOrCancelledToken);
                    break;
                case Func<Task> asyncOperation:
                    operationTask = asyncOperation();
                    break;
                case Action syncOperation:
                    operationTask = Task.Run(() => syncOperation(), timeoutOrCancelledToken);
                    break;
                default:
                    throw new NotSupportedException($"Operation of type {Operation.GetType()}");
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
            }
            else
            {
                await operationTask.ConfigureAwait(false);
            }
        }
    }
}
