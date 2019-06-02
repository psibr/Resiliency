using Resiliency.BackoffStrategies;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ActionOperation = System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task>;

namespace Resiliency
{
    /// <summary>
    /// Represents the absence of a value.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size=0, CharSet=CharSet.Ansi)]
    internal readonly struct Unit : IEquatable<Unit>
    {
        public bool Equals(Unit other) => true;

        public override bool Equals(object obj) => obj is Unit;

        public override int GetHashCode() => 0;

        public override string ToString() => "()";
    }

    public class ResilientActionBuilder
    {
        internal ResilientActionBuilder(Func<CancellationToken, Task<Unit>> action, int sourceLineNumber, string sourceFilePath, string memberName)
        {
            ResilientFuncBuilder = new ResilientFuncBuilder<Func<CancellationToken, Task<Unit>>, Unit>(action, sourceLineNumber, sourceFilePath, memberName);
        }

        private ResilientFuncBuilder<Func<CancellationToken, Task<Unit>>, Unit> ResilientFuncBuilder { get; }

        public ResilientActionBuilder TimeoutAfter(TimeSpan period)
        {
            ResilientFuncBuilder.TimeoutAfter(period);

            return this;
        }

        public ResilientActionBuilder WhenExceptionIs<TException>(
            Func<IResilientOperation, TException, Task> handler)
            where TException : Exception
        {
            ResilientFuncBuilder.WhenExceptionIs(handler);

            return this;
        }

        public ResilientActionBuilder WhenExceptionIs<TException>(
            Func<TException, bool> condition,
            Func<IResilientOperation, TException, Task> handler)
            where TException : Exception
        {
            ResilientFuncBuilder.WhenExceptionIs(condition, handler);

            return this;
        }

        public ResilientActionBuilder WithCircuitBreaker(string operationKey, Func<CircuitBreaker> onMissingFactory)
        {
            ResilientFuncBuilder.WithCircuitBreaker(operationKey, onMissingFactory);

            return this;
        }

        public ActionOperation GetOperation()
        {
            return InvokeAsync;
        }

        public Task InvokeAsync(CancellationToken cancellationToken = default)
        {
            return ResilientFuncBuilder.InvokeAsync(cancellationToken);
        }
    }
}
