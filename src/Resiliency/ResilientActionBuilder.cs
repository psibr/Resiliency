using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActionOperation = System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task>;

namespace Resiliency
{

    using ActionOperationWrapper = Func<ActionOperation, ActionOperation>;

    public class ResilientActionBuilder<TAction>
        : ResilientOperationBuilder<TAction>
    {
        protected List<ActionOperationWrapper> CircuitBreakerHandlers = new List<ActionOperationWrapper>();

        internal ResilientActionBuilder(TAction action, int sourceLineNumber, string sourceFilePath, string memberName)
            : base(action, sourceLineNumber, sourceFilePath, memberName)
        {
            // Add an implicit operation circuit breaker that must be explicitly tripped.
            // This is comonly used for 429 and 503 style exceptions where you KNOW you have been throttled.
            WithCircuitBreaker(ImplicitOperationKey, () => new CircuitBreaker(new ExplicitTripCircuitBreakerStrategy()));
        }

        public new ResilientActionBuilder<TAction> WhenExceptionIs<TException>(
            Func<ResilientOperation, TException, Task> handler)
            where TException : Exception
        {
            base.WhenExceptionIs(handler);

            return this;
        }

        public new ResilientActionBuilder<TAction> When(
            Func<Exception, bool> condition,
            Func<ResilientOperation, Exception, Task> handler)
        {
            base.When(condition, handler);

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

            var partiallyAppliedHandlers = new List<Func<Exception, Task<HandlerResult>>>();

            // Prepare handlers
            foreach (var handler in Handlers)
            {
                var op = new ResilientOperation(ImplicitOperationKey, new ResilientOperationHandlerInfo(), totalInfo, cancellationToken);

                partiallyAppliedHandlers.Add((ex) => handler(op, ex));
            }

            do
            {
                try
                {
                    await wrappedOperation(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    try
                    {
                        await ProcessHandlers(partiallyAppliedHandlers, ex, cancellationToken);
                    }
                    catch (CircuitBrokenException circuitBrokenEx)
                    {
                        await ProcessHandlers(partiallyAppliedHandlers, circuitBrokenEx, cancellationToken);
                    }
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
