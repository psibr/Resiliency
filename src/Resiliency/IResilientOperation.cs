using System.Threading;

namespace Resiliency
{
    public interface IResilientOperation
    {
        CancellationToken CancellationToken { get; }
        int CurrentAttempt { get; }
        CircuitBreaker DefaultCircuitBreaker { get; }
        ResilientOperationHandlerInfo Handler { get; }
        string ImplicitOperationKey { get; }

        void Break();
        void Retry();
    }

    public interface IResilientOperation<TResult>
        : IResilientOperation
    {
        void Return(TResult result);
    }
}